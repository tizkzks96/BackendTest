from fastapi import FastAPI, Request, Form
from fastapi.responses import HTMLResponse
from fastapi.templating import Jinja2Templates
from fastapi.staticfiles import StaticFiles

app = FastAPI()

# 정적 파일과 템플릿 디렉토리 설정
app.mount("/static", StaticFiles(directory="app/static"), name="static")
templates = Jinja2Templates(directory="app/templates")

@app.get("/login", response_class=HTMLResponse)
def login_page(request: Request):
    return templates.TemplateResponse("login.html", {"request": request})

@app.post("/login")
def login(email: str = Form(...), password: str = Form(...)):
    if email == "test@example.com" and password == "password123":
        return {"message": "Login successful"}
    return {"message": "Invalid credentials"}
