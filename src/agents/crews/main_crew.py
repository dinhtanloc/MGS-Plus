from __future__ import annotations

from typing import Optional

from crewai import Crew, Process

from src.agents.core.config import Settings, get_settings
from src.agents.core.memory.memory_service import MemoryService
from src.agents.agents.documents.agent import build_documents_agent
from src.agents.agents.supervisor.agent import build_supervisor_agent
from src.agents.agents.workflow.agent import build_workflow_agent
from src.agents.crews.tasks.supervisor_tasks import route_and_respond_task


class MainCrew:
    """Assembles the full multi-agent crew with hierarchical process.

    Process flow:
      User → Supervisor (manager_agent) → delegates → Documents / Workflow agents
                                        ← collects results
      Supervisor → synthesised answer → User

    A2A note:
      - Within a single server process, CrewAI hierarchical delegation is used.
      - When services run independently (separate ports), the Supervisor's
        routing tools use the A2AClient to call sub-agent HTTP endpoints.
    """

    def __init__(self, settings: Optional[Settings] = None) -> None:
        self._settings = settings or get_settings()
        self._memory = MemoryService(self._settings)

        # Build agents once — reuse across requests
        self._supervisor = build_supervisor_agent(self._settings)
        self._documents = build_documents_agent(self._settings)
        self._workflow = build_workflow_agent(self._settings)

    def _build_crew(self, question: str, user_id: str, context: str = "") -> Crew:
        """Build a fresh Crew for each request (tasks are request-scoped)."""
        task = route_and_respond_task(
            supervisor=self._supervisor,
            question=question,
            context=context,
        )
        return Crew(
            agents=[self._documents, self._workflow],
            tasks=[task],
            process=Process.hierarchical,
            manager_agent=self._supervisor,
            verbose=True,
        )

    def kickoff(
        self,
        question: str,
        thread_id: str,
        user_id: str = "anonymous",
    ) -> str:
        """Run the crew synchronously and persist the exchange in memory."""
        # Load context from short-term memory to enrich the task
        history = self._memory.load_context(thread_id)
        context = "\n".join(
            f"{m['role'].upper()}: {m['content']}" for m in history[-10:]
        )

        crew = self._build_crew(question, user_id, context)
        result = crew.kickoff()
        answer = str(result)

        # Persist the exchange
        self._memory.append(thread_id, user_id, "user", question)
        self._memory.append(thread_id, user_id, "assistant", answer)

        # Trigger summarisation if threshold reached
        if self._memory.should_summarise(thread_id):
            self._summarise_and_persist(thread_id, user_id)

        return answer

    def _summarise_and_persist(self, thread_id: str, user_id: str) -> None:
        """Ask the supervisor LLM to summarise the thread and persist to long-term memory."""
        messages = self._memory.load_context(thread_id)
        transcript = "\n".join(f"{m['role']}: {m['content']}" for m in messages)
        summary_prompt = (
            f"Summarise the following conversation in 3-5 sentences for long-term memory:\n\n"
            f"{transcript}"
        )
        # Use the supervisor's LLM directly for summarisation
        summary = self._supervisor.llm.call([{"role": "user", "content": summary_prompt}])
        self._memory.upsert_long_term(user_id=user_id, summary=str(summary))
        self._memory.clear_thread(thread_id)
