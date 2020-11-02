# 목차
- [Vitess가 무엇인가?](#vitess가-무엇인가)
- [기능](#기능)
- [모니터링](#모니터링)
- [형상 관리 툴](#형상-관리-툴)
- [샤딩(수평 분할)](#샤딩수평-분할)
- [Vitess Vs. NoSQL](#vitess-vs-nosql)

### 번역 사이트 : https://vitess.io/docs/overview/whatisvitess/
###### <br>
# 본문
## Vitess가 무엇인가?
### Vitess는 오픈 소스 데이터 베이스 인스턴스의 대규모 클러스터를 배포, 확장, 관리하기 위한 데이터 베이스 솔루션입니다. 
###  MySQL과 MariaDB를 지원합니다. public또는 private 클라우드 구조에서 효과적으로 수행되도록 구조화되 있습니다.
### NoSQL 데이터 베이스의 확장성과 많은 중요한 SQL 기능을 결합하고 확장합니다. 
### Vitess는 다음과 같은 문제에 도움이 됩니다.
1. 애플리케이션의 최소한의 변경으로 분할을 지원하여 SQL 데이터 베이스를 확장합니다. 
2. 베어메탈에서 private 또는 public 클라우드로 개선 
   * (베어메탈의 뜻은 운영체제가 설치되어 있지 않는 컴퓨터 하드웨어를 의미함)
3. 많은 수의 SQL 데이터베이스 인스턴스들을 배포와 관리 

### vitess는 native 쿼리 프로토콜을 사용하는 JDBC와 GO 데이터베이스 드라이버를 포함합니다. 
### 사실상 다른 모든 언어와 호환되는 MySQL 서버 프로토콜을 구현됩니다.
### Vitess는 2011년부터 모든 YouTube 데이터베이스 트래픽을 제공하고 있고 현재 많은 기업에서 채택하고 있습니다.
###### <br>
## 기능
### 성능
* Connection Pool 
  * 성능을 최적화하기 위해서 MYSQL connection Pool에 프런트-엔드 애플리케이션 쿼리를 다중화합니다.
    * (다중화(multiplex)란 여러개의 신호를 하나 채널에 처리하는 것 ex) 시분할 다중화, 주파수 분할 다중화)
    * (이곳에서 다중화의 의미는 pool을 사용해서 여러개의 쿼리를 다중화해서 사용하겟다는 뜻 즉 connection을 재사용하겠다는 의미인듯)
* 쿼리 중복 제거 
  * 진행중인 쿼리가 계속 실행되는 동안 수신된 동일한 요청에 대해서 진행중인 쿼리의 결과를 재사용합니다. 
* Transaction manager 
  * 동시 transaction 수를 제한하고 전체 처리량을 최적화하기 위해서 deadline을 관리합니다. 
### 안정성
* 쿼리 재작성과 최적화 
  * 제한을 추가하고 비 결정적인 업데이트를 방지합니다. 
  * appendix
    * 결정적인 함수 
      * 데이터 베이스의 상태가 같을 경우 특정 Input이 동일한 output 반환
      * 예) 평균값
    * 비 결정적인 함수 
      * 데이터 베이스의 상태가 같을 경우 특정 Input이 다른 output 반환
      * 예) 날짜/시간 반환
* 쿼리 블랙리스트
  * 잠재적으로 문제가 있는 쿼리가 데이터 베이스에 전달되지 않도록 사용자가 규칙을 지정합니다. 
* 쿼리 종료
  * 데이터를 반환이 너무 오래 걸리는 쿼리를 종료합니다. 
* Table ACLS
  * 접속된 유저를 기반으로 테이블에 대한 ACL을 지정합니다. 
    * ACLs이란
      * Access control List를 뜻함 
      * 리스트에 등록되어 있는 정보로 Access를 제어함(기본적인 방화벽기능 지원)
      * standard ACL
        * L3헤더(IP헤더)의 Source Address를 조건으로 패킷을 분류
      * Extended ACL
        * L3헤더(IP헤더)의 Source Address, Dest Address, Protocol, TTL정보와 L4(TCP/UDP)의 Source port, Dest port, TCP Flag등의 정보를 기반으로 패킷을 분류
###### <br>
## 모니터링
* 성능 분석 툴을 사용하면 데이터 베이스 성능을 모니터링, 진단 및 분석 할수 있습니다.
###### <br>
## 형상 관리 툴
* 마스터 관리 툴
* 웹 기반 관리 GUI
* 여러 데이터 센터 / 지역에서 작동하도록 설계 
###### <br>
## 샤딩(수평 분할)
* 심리스한 동적 재 sharding
  * 심리스 : 경계가 보이지 않은
* 수평적, 수직적 sharding 지원
* 사용자가 plug-in 할 수 있는 다중 sharding 구조

## Vitess Vs. NoSQL
### 만약에 MySQL의 확장성에 대한 걱정 때문에 NoSQL 솔루션을 고려하고 있다면 Vitess는 애플리케이션에 더 적합한 선택일 수 있습니다. 
### NoSQL은 비정형 데이터에 대해서 훌륭하게 지원하지만 Vitess는 NoSQL 저장소에서 가능하지 않은 다양한 이점들을 제공합니다. 
|NoSQL|Vitess|
|:--|:--|
|NoSQL 데이터베이스는데이터 베이스 테이블들<br>사이의 관계에 대해서 정의하지 않습니다.|Vitess는 단순한 key=value 저장소가 아닙니다.<br>Where절과 JOINS, 집계함수같은<br>복잡한 쿼리문법을 지원합니다.|
|NoSQL 데이터베이스는 일반적으로<br>transaction을 지원하지 않습니다.|Vitess는 transaction을 지원합니다.|
|NoSQL 해법은 Custom한 API가 있어서 <br>Custom한 구조, 애플리케이션, 도구를 사용합니다.|Vitess는 이미 많은사람들이 익숙한 데이터 베이스인<br>Mysql에 거의 변화를 주지 않습니다.|
|NoSQL 해법은 MySQL과 비교해서 데이터 베이스<br>인덱스에 대한 제한된 지원을 제공합니다.|Vitess를 사용하면 MySQL의 모든 인덱스기능을<br>사용해서 쿼리의 성능을 최적화 할 수 있습니다. |
