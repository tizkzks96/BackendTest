# app/main.py
from fastapi import FastAPI
from app.dependencies import templates
from app.routers import auth, get_root
from fastapi.staticfiles import StaticFiles

app = FastAPI()

# 정적 파일 설정
app.mount("/static", StaticFiles(directory="app/static"), name="static")

# 라우터 포함
app.include_router(auth.router)
app.include_router(get_root.router)