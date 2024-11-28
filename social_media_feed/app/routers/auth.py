# app/routers/auth.py
from fastapi import APIRouter, Request, Form
from fastapi.responses import HTMLResponse, RedirectResponse
from app.dependencies import templates
from passlib.context import CryptContext

router = APIRouter()

@router.get("/login", response_class=HTMLResponse)
def login_page(request: Request, login_failed: bool = False):
    """
    로그인 페이지 렌더링
    - login_failed: 로그인 실패 여부를 알림 팝업에 표시
    """
    return templates.TemplateResponse("login.html", {"request": request, "login_failed": login_failed})

@router.post("/login")
def login(email: str = Form(...), password: str = Form(...)):
    """
    로그인 처리
    - 성공: /success로 리디렉션
    - 실패: login_failed=True로 /login 페이지를 다시 렌더링
    """
    if email == "test@example.com" and password == "password123":
        return RedirectResponse(url="/success", status_code=302)
    return RedirectResponse(url="/login?login_failed=true", status_code=302)

@router.get("/success", response_class=HTMLResponse)
def success_page(request: Request):
    """
    로그인 성공 시 표시되는 페이지
    """
    return templates.TemplateResponse("success.html", {"request": request})

# 패스워드 해싱을 위한 설정
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

# 간단한 사용자 데이터 저장 (예시로 딕셔너리 사용)
users_db = {}

# 회원가입 페이지 렌더링
@router.get("/signup", response_class=HTMLResponse)
def signup_page(request: Request):
    return templates.TemplateResponse("signup.html", {"request": request})

# 회원가입 처리
@router.post("/signup")
def signup(request: Request, email: str = Form(...), password: str = Form(...)):
    if email in users_db:
        # 이미 존재하는 이메일인 경우 에러 메시지를 전달
        return templates.TemplateResponse("signup.html", {"request": request, "error": "Email already exists."})
    # 비밀번호 해싱
    hashed_password = pwd_context.hash(password)
    # 사용자 데이터 저장
    users_db[email] = {"email": email, "hashed_password": hashed_password}
    # 회원가입 성공 시 로그인 페이지로 리디렉션
    return RedirectResponse(url="/login", status_code=302)