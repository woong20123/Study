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
 # mysql 서비스 종료
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

## mysql insert 속도 향상
* innodb_flush_log_at_trx_commit 기능을 끄면 로그 파일을 기록하지 않음


## mysql exp_source 테이블 인덱스 적용시 속도
### 테스트 방식
spring boot 서버에서 cassandra에 접근하여 데이터 가져오기
가져온 데이터를 ResultDB에 insert하기 위한 작업 실행
manager에서 유니크한 id를 shuffle을 통해서 랜덤 insert
### 속도저하 측정
테스트 테이블 구조
```sql
CREATE TABLE `tempdata` (
  `A` bigint NOT NULL,
  `B` varchar(50) NOT NULL,
  `C` int NOT NULL,
  `D` bigint NOT NULL,
  `E` varchar(30) NOT NULL,
  `F` bigint NOT NULL,
  PRIMARY KEY (`A`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
```

#### innodb_buffer_pool_size = 10G  지정시 

|insert 갯수|cassandra select|result DB bulk insert|pbn 데이터 베이스 크기|
|:--:|:--:|:--:|:--:|
|83만8581개|26.98 secs|11.7 secs||
|125만7917개|40.35 secs|16.95 sec||
|167만7395개|55.24 secs|22.86 secs||
|335만4644개|107.50 secs|48.28 secs||
|667만6109개|216.23 secs|95.25 secs||
|1331만9183 개|440.65 secs|197.37 secs|1.7G|

* 램이 128MB일때 120만건에서 지연 현상 발생
* 1G는 128MB의 8배 = 1000만건
* 10G는 128MB의 80배 = 1억건 정도 예상됨

#### innodb_buffer_pool_size = 1G  지정시 
|insert 갯수|cassandra select|result DB bulk insert|pbn 데이터 베이스 크기|
|:--:|:--:|:--:|:--:|
|335만4644개|107.50 secs|48.28 secs||
|667만6109개|216.23 secs|95.25 secs||
|1331만9183 개|440.65 secs|197.37 secs|1.7G|

### innodb_buffer_pool_size default(128MB) 일때 

|insert 갯수|cassandra select|result DB bulk insert|
|:--:|:--:|:--:|
|41만9173개|13.75 secs|6.89 secs|
|83만8581개|26.98 secs|13.23 secs|
|125만7917개|40.35 secs|22.83 sec|
|148만4194개|48.35.secs|60.sec|
|167만7395개|55.24 secs|130.76 secs|

현재 발생한 내용
 130만개가 넘어가면서 insert의 속도 효율이 떨어짐.

