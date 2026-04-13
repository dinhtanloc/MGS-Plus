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
def get_engine(settings: Settings | None = None):
    from src.agents.core.config import get_settings
    return build_engine(settings or get_settings())


def get_session_factory(settings: Settings | None = None) -> sessionmaker:
    engine = get_engine(settings)
    return sessionmaker(bind=engine, expire_on_commit=False)


def get_db_session(settings: Settings | None = None) -> Session:
    """Yield a DB session (use as context manager or dependency)."""
    factory = get_session_factory(settings)
    return factory()
