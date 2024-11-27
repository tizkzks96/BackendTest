# FastAPI 엔트리포인트

from fastapi import FastAPI

app = FastAPI()

@app.get('/')
def read_root():
    return {'message': 'Welcome to FastAPI!'}
