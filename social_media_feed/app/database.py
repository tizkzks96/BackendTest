from sqlalchemy import create_engine, Column, Integer, String, DateTime
from sqlalchemy.orm import declarative_base, sessionmaker
from datetime import datetime

DATABASE_URL = "postgresql+psycopg2://postgres:0694@localhost/postgres"
engine = create_engine(DATABASE_URL, echo=True)

# Base 클래스 생성
Base = declarative_base()

# 세션 구성
SessionLocal = sessionmaker(bind=engine, autocommit=False, autoflush=False)

# SQLAlchemy 모델 정의
class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    username = Column(String, unique=True, nullable=False)
    password_hash = Column(String, nullable=False)
    created_at = Column(DateTime, default=datetime.utcnow)
    email = Column(String, unique=False, nullable=True) 

# Alembic으로 테이블 관리
print("테이블 관리는 Alembic을 사용하세요.")

# 데이터 삽입 및 세션 생성
def create_user():
    session = SessionLocal()
    new_user = User(
        username="test_user",
        password_hash="hashed_password_example"
    )

    try:
        session.add(new_user)
        session.commit()
        print("success!")
    except Exception as e:
        session.rollback()
        print(f"fail: {e}")
    finally:
        session.close()

if __name__ == "__main__":
    create_user()
