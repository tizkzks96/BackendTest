from fastapi import FastAPI, Request, Form
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates
from fastapi.staticfiles import StaticFiles

app = FastAPI()

# 정적 파일과 템플릿 디렉토리 설정
app.mount("/static", StaticFiles(directory="app/static"), name="static")
templates = Jinja2Templates(directory="app/templates")

@app.get("/login", response_class=HTMLResponse)
def login_page(request: Request, login_failed: bool = False):
    """
    로그인 페이지 렌더링
    - login_failed: 로그인 실패 여부를 알림 팝업에 표시
    """
    return templates.TemplateResponse("login.html", {"request": request, "login_failed": login_failed})

@app.post("/login")
def login(email: str = Form(...), password: str = Form(...)):
    """
    로그인 처리
    - 성공: /success로 리디렉션
    - 실패: login_failed=True로 /login 페이지를 다시 렌더링
    """
    if email == "test@example.com" and password == "password123":
        return RedirectResponse(url="/success", status_code=302)
    return RedirectResponse(url="/login?login_failed=true", status_code=302)

@app.get("/success", response_class=HTMLResponse)
def success_page(request: Request):
    """
    로그인 성공 시 표시되는 페이지
    """
    return templates.TemplateResponse("success.html", {"request": request})
