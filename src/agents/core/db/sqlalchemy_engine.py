from __future__ import annotations

from functools import lru_cache

from sqlalchemy import create_engine
from sqlalchemy.orm import DeclarativeBase, sessionmaker, Session
from sqlalchemy.pool import NullPool

from src.agents.core.config import Settings


class Base(DeclarativeBase):
    pass


def build_engine(settings: Settings):
    """Synchronous SQLAlchemy engine for SQL Server via pymssql."""
    return create_engine(
        settings.sqlserver_url,
        poolclass=NullPool,
        echo=False,
    )


@lru_cache(maxsize=1)
def get_engine():
    from src.agents.core.config import get_settings
    return build_engine(get_settings())


def get_session_factory(settings: Settings | None = None) -> sessionmaker:
    return sessionmaker(bind=get_engine(), expire_on_commit=False)


def get_db_session(settings: Settings | None = None) -> Session:
    """Return a DB session (use as context manager)."""
    return get_session_factory()()


def init_db() -> None:
    """Create all agent tables if they don't exist yet (idempotent)."""
    import src.agents.core.memory.models  # noqa: F401 — registers models with Base
    Base.metadata.create_all(get_engine())

