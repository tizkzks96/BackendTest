# app/routers/get_root.py
from fastapi import APIRouter

router = APIRouter()

@router.get("/")
def read_root():
    return {"message": "Hello, GCP without Docker!"}
