import os

# 디렉토리와 파일 구조 정의
structure = {
    "social_media_feed": {
        "app": {
            "main.py": "# FastAPI 엔트리포인트\n\nfrom fastapi import FastAPI\n\napp = FastAPI()\n\n@app.get('/')\ndef read_root():\n    return {'message': 'Welcome to FastAPI!'}\n",
            "models.py": "# 데이터베이스 모델 정의\n\n# Define your SQLAlchemy models here.",
            "database.py": "# 데이터베이스 연결 설정\n\n# Define your database connection and session setup here.",
            "schemas.py": "# Pydantic 스키마 정의\n\n# Define your request and response schemas here.",
            "utils.py": "# JWT 유틸리티 함수\n\n# Define your JWT helper functions here.",
            "routes": {
                "auth.py": "# 사용자 인증 API\n\n# Define your authentication endpoints here.",
                "posts.py": "# 게시물 API\n\n# Define your post-related endpoints here.",
                "follows.py": "# 팔로우 API\n\n# Define your follow/unfollow endpoints here."
            }
        },
        "tests": {},  # 테스트 코드 디렉토리
        "requirements.txt": "# Python 종속성 파일\n\nfastapi\nuvicorn\nsqlalchemy\npydantic\n",
        "README.md": "# 프로젝트 설명\n\nThis is a social media feed service built with FastAPI."
    }
}

# 디렉토리와 파일 생성 함수
def create_structure(base_path, structure):
    for name, content in structure.items():
        path = os.path.join(base_path, name)
        if isinstance(content, dict):  # 디렉토리 생성
            os.makedirs(path, exist_ok=True)
            create_structure(path, content)
        else:  # 파일 생성
            with open(path, "w") as f:
                f.write(content)

# 스크립트 실행
if __name__ == "__main__":
    base_path = os.getcwd()  # 현재 디렉토리 기준
    create_structure(base_path, structure)
    print("디렉토리 구조 생성 완료!")