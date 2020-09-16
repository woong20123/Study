## ubuntu 18에 mysql 8버전 설치하기 
* 아래 로직 수행
```java
sudo wget https://dev.mysql.com/get/mysql-apt-config_0.8.15-1_all.deb
sudo dpkg -i mysql-apt-config_0.8.15-1_all.deb
sudo apt-get update
 sudo apt-get install mysql-server
```

## 만약 비밀번호를 읽어버린 경우
```sql
-- 관리자로 접속해서 
sudo mysql
-- 아래 로직 수행
>> ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY 'root';
```

## 외부 접속 가능하도록 설정
```sql
>> ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY 'root';
```

## 원격의 mysql서버 명령어로 접속 
```sql
mysql -h 172.19.153.126 -u root -p
```

## 계정에 데이터 베이스 접근 권한 주기
```sql
GRANT ALL ON TempDB.* TO 'root'@'%';
GRANT ALL ON ScheduleDB.* TO 'root'@'%';
flush privileges
```