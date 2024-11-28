from fastapi import APIRouter, Depends
from typing import List
from app.schemas.user import User
from app.models.user import User as UserModel

router = APIRouter(
    prefix="/users",
    tags=["users"],
)

@router.get("/", response_model=List[User])
def read_users():
    # 사용자 목록 조회 로직
    pass

@router.post("/", response_model=User)
def create_user(user: User):
    # 사용자 생성 로직
    pass
