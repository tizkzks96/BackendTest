from hashlib import sha256

# 비밀번호 해싱
def hash_password(password):
    return sha256(password.encode()).hexdigest()

# 사용자 데이터 추가
new_user = User(
    username="test_user",
    password_hash=hash_password("secure_password123")
)

session.add(new_user)
session.commit()
print("새 사용자 추가 완료!")