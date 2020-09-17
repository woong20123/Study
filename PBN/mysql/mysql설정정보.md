## ubuntu 18에 mysql 8버전 설치하기 
* 아래 로직 수행
```java
sudo wget https://dev.mysql.com/get/mysql-apt-config_0.8.15-1_all.deb
sudo dpkg -i mysql-apt-config_0.8.15-1_all.deb
sudo apt-get update
 sudo apt-get install mysql-server
```

## mysql 서비스 관리
```bash
sudo systemctl start mysql.service # mysql 서비스 시작
sudo systemctl stop mysql.service # mysql 서비스 종료
sudo systemctl status mysql.service # mysql 서비스 상태 출력
```

## 만약 비밀번호를 읽어버린 경우
```sql
-- 관리자로 접속해서 
sudo mysql
-- 아래 로직 수행
>> ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY 'root';
```

## 외부 접속 가능하도록 설정
#### 데이터 베이스의 유저 확인
```sql
use mysql;
select user, host from user;
```
```sql
-- 모든곳에서 접근할 수 있는 root, 비밀번호 : root인 계정 생성
create user 'root'@'%' identified by 'root';
```
### Ubuntu머신에서 접근 테스트 
* 아래의 예제는 다른 ubuntu머신에 접근 하는 예제
```bash
# 클라이언트용 mysql 패키지를 설치합니다.
sudo apt-get intall mysql-client
# -h 목적지 주소를 붙입니다.
mysql -h 172.19.153.126 -u root -p
```
## 계정에 데이터 베이스 접근 권한 주기
```sql
GRANT ALL ON TempDB.* TO 'root'@'%';
GRANT ALL ON ScheduleDB.* TO 'root'@'%';
flush privileges
```