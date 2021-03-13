# MongoDB의 Data Modeling
* 개념적 데이터 모델링 : 비즈니스 영역에서 데이터를 수집
  * collection 추출
  * Field 추출
  * Document & Tree Doc 추출
  * Link/Embedded 추출
  * Collection Diagram 작도
* 논리적 데이터 모델링 : Document 구조에 맞게 분석, 설계 
  * Data Type 결정
  * Validator 설계 
* 물리적 데이터 모델링 : MongoDB 물리적 구조에 맞도록 설계 진행
  * Database 설계
  * 사용자 계정 설계
  * Collection 타입/크기 설계 

## MongoDB 설계 주요 특징 
* NoSQL 에서는 데이터 중복을 허용하며 역정규화된 설계를 지향합니다. 
  * 저장 장치의 비약적인 발전과 저렴한 가격 덕분
* Rich Document Structure 제공
* 중첩 구조 제공 

## MongoDB 설계 기준
* 데이터 조작은 어떻게 수행되는가?
  * 하나의 Collection은 여러 필드로 구성됨
  * 모든 필드의 접근 횟수가 일정하지 않음 
  * 접근 횟수가 비슷한 필드를 collection에 묶어 놓아야 자원 낭비가 덜함 
* Access Pattern은 어떤가?
  * 얼마나 자주 READ,WRITE 되는가?
    * 쓰기가 빈번한 필드를 Collection은 묶어서 관리
    * 읽기가 빈번한 필드를 Collection으로 묶어서 관리
  * 얼마나 보관되어야 하는가?

## MongoDB 설계 패턴
* 객체 지향 데이터 베이스
  * Strong Association : 밀접한 관계를 가진 경우 표현
    * 중첩된 테이블로 표현 됩니다. 
  * Weak Association : 밀접하지 않은 관계를 가진 경우 표현 
    * 테이블 안에 배열로 저장되어 관리됩니다. 

## Embedded Document
* 관계형 DB와는 다르게 조인 문장을 사용하지 않음
* Document안에 내장형 Document로 표현합니다. 