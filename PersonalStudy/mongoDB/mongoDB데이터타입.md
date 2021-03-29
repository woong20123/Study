# JSON 타입과 BSON 타입
## JSON(Java Script Object Notation) 타입
## BSON(Binary Serial Object Notation) 타입
* MongoDB에서 JSON타입으로 표현되는 데이터는 BSON 타입으로 저장 됩니다. 
# 데이터 타입 종류 
## Object타입
* RDB의 rowID와 유사한 데이터 속성입니다. 
* 하나의 document에서 구별될 수 있는 유일한 값입니다. 
* BSON Object ID는 12Byte의 Binary값으로 구성
* Collection에 Document가 추가되면 ObjectID가 부여됩니다. 
## JSON 타입
* RDBMS의 문자, 숫자, 바이너리 데이터 저장용

|Type Name|Type Number|
|:--|:--|
|Double|1|
|String|2|
|Object|3|
|Array|4|
|Binary Data|5|
|Object id|7|
|Boolean|8|
|Date|9|
|Null|10|
|Regular expression|11|
|JavaScript code|13|
|Symbol|14|
|JavaScript code with scope|15|
|32-bit Integer|16|
|Timestamp|17|
|64-bit integer|18|
|Min key|255|
|Max key|127|

## 연산자 종류 

### 비교 연산자

|유형|설명|
|:--|:--|
|$cmp|두개의 값을 비교하여 첫번째 인수가 크면 `음수(-)`, 크면 `양수(+)` 같으면 `0`을 리턴|
|$eq|두개의 값을 비교하여 동일하면 True, 다르면 False|
|$gt|두개의 값을 비교하여 크면 True|
|$gte|두개의 값을 비교하여 크거나 같은 True|
|$lt|두개의 값을 비교하여 작으면 True|
|$lte|두개의 값을 비교하여 작거나 같으면 True|
|$ne|두개의 값을 비교하여 같지 않으면 True|

### Boolean 연산자

|유형|설명|
|:--|:--|
|$and|여러 조건이 모두 True인 경우|
|$not|검색 조건이 아닌 조건을 검색|
|$or|여러 개의 조건중에 하나라도 만족 |

### 예제 소스
```js
// select * from employees where empno = 7369
db.employees.find( {empno : 7369}).forEach(printjson) 

// select ename from employees where empno = 7900
db.employees.find({empno: 7900}, {ename : 1}).forEach(printjson)

// select empno, ename from employees where ename >= 'ALLEN' and ename < 'SCOTT'
db.employees.find({}, {_id : 0, empno: 1, ename : 1}).min({ename : "ALLEN"}).max({ename : "SCOTT"})

// select empno, ename from employees where empno > 7500 and empno <= 7600
db.employees.find({empno : {$gt: 7500, $lte : 7600}, {_id :0, empno : 1, ename : 1}})

// select empno from employees where empno=7782 or empno=7844
db.employees.find({ $or : [{empno:7782}, {empno:7844}]}, {_id : 0, empno:1})

// selct count(*) from employees
db.employees.count()

// select distinct deptno from employees;
db.employees.distinct("deptno")

// select ename, job from employees whre deptno=10 order by ename desc;
db.employees.find( {deptno : 10}, { _id : 0, ename : 1, job : 1}).sort({ename : -1})

// select empno, ename  from employees where sal <= 1000
db.employees.find({ sal : {$lte : 1000}}, {_id : 0, empno : "", ename : ""})

// select empno, deptno from employees where deptno = 10 and sal > 1000
db.employees.find( { $and : [{deptno : 10}, {sal : {$gt : 1000}}])}, {_id : 0, empno : 1, deptno : 1})
```

## 변경 연산자

### 산술 연산자

|유형|설명|
|:--|:--|
|$add| 플러스 연산자 |
|$devide| 나누기 연산자|
|$mod| 나머지 연산자 |
|$multiply| 곱하기 연산자|
|$subtract| 빼기 연산자 |

### 문자 연산자

|유형|설명|
|:--|:--|
|$strcasecmp| Long 타입의 긴 문자열 2개를 비교함 첫번째 문자열이 크면 양수, 작으면 0|
|$substr| c++ substr |
|$toUpper| 대문자 |
|$toLower| 소문자 |

* 예제 코드
```js
// $type 1은 Double을 뜻함 => comm이 double인 값을 리턴
db.employees.find({ comm : { $type : 1}} }, { empno : "", comm : ""} )

// 정규 표현식 = i : 대소문자 구분 안함
db.employees.find( {ename: { $regex : 'S.*H', $option : 'i'} }, {empno : "", ename : ""})
```

## SQL과 mongoDB 문법 비교
### Create 문
```js
// SQL
CREATE TABLE members(mem_no varchar(30), age number, type char(1), PRIMARY KEY(mem_no) );
// MongoDB 스키마
db.member.insert({ mem_no : "111", age : 49, type : "ACE"}) or db.createCollection("membes")

// SQL
CREATE INDEX i_members_type on members(type)
// MongoDB 스키마
db.members.ensureIndex({ type : 1 })

// SQL
DROP TABLE members;
// MongoDB 스키마
db.members.drop();
```
### Update 문
```js
// SQL
UPDATE members SET type = "GOLD" WHERE age > 45
// MongoDB 스키마
db.members.update({ age : { $gt : 45 } }, { $set : {type : "GOLD"} }, {multi : true} )

// SQL 
UPDATE members SET age = age + 3 WHERE type = "ACE"
// MongoDB 스키마
db.members.update( { type : "ACE"}, { $inc : {age : 3} }, {multi : true} )
```

### DELETE 문
```js
// SQL 
DELETE FROM members WHERE type = "ACE"
// MonjgoDB 스키마
db.members.remove( { type : "ACE" })
```

## 빅데이터 추출과 분석 
### Aggregation FrameWork
* 최소한의 코딩과 빠른 읽기 작업을 가능하게 만든 빅 데이터 추출 기능
### MongoDB의 MapReduce
* MongoDB 초기 버전때부터 제공된 Map, Reduce를 통한 javascript 문법 처리 기능 
### Hadoop Map/Reduce와 MongoDB의 연동 

## Aggregation 프레임 워크
* Aggregation을 사용하게 되면 내부적으로 MongoDB의 MapReduce를 사용하여 빠른 성능을 보여줍니다.
* Aggregation 함수는 $`project`(SELECT), $`match`(WHERE), $`group`(GROUP BY), $`sort`(ORDER BY), $`limit`, $`skip` 연결 연산자 사용

## Aggregate 함수 사용 예제
```js
// SQL
SELECT COUNT(*) AS count FROM order

// mongo db
db.order.aggregate([ { $group : { _id : null, count : { $sum : 1} } } ])

// SQL
SELECT SUM(price) AS total_price FROM order
// mongo db
db.order.aggregate([
    {$group : { _id : null, total_price : { $sum : $price } } }
] )

// SQL 
SELECT cust_id SUM(price) AS total_price FROM order
GROUP BY cust_id
// mongo db
db.order.aggregate( [
{ 
    $group : { _id : "$cust_id", total_price : {$sum : "$price" } }}, { $sort : {total_price : 1}}
])

// SQL
SELECT cust_id, ord_date, SUM(price) AS total_price FROM order
GROUP BY cust_id, ord_date
// mongo db
db.order.aggregate([
    { $group : {
        _id : { cust_id : "$cust_id", ord_date : $"ord_date" },
        total_price : { $sum : "$price"}
    }}
])

// SQL 
SELECT cust_id, count(*) FROM order
GROUP BY cust_id 
HAVING count(*) > 1
// mongo db
db.order.aggregate([
    { $group : { _id :  "$cust_id", count : { $sum : 1} } },
    { $match : { count : { $gt : 1 } } }
])
```

## MapReduce 기능
* MapReduce란 구글에서 대용량 데이터의 효율적인 처리 및 데이터 집합을 위해 만들어짐 
* MongoDB에서 MapReduce는 빅 데이터에 대한 빠른 읽기 및 집계를 통해서 통계 정보 분석 추출
### MapReduce 문법
```js
db.collection_name.mapReduce(
    <map>,                      map 함수 이름
    <reduce>,                   reduce 함수 이름
    {
        <out>,                  실행 결과를 저장할 collection명
        <query>,                검색 조건
        <sort>,                 sorting 조건
        <limit>,                데이터 검색 조건
        <finalize>,             실행 결과를 집계할 함수이름
        <scope>,
        <jsMode>,
        <verbose>
    }
)
```
## JavaScript 함수
### 비저장형 
* MongoDB 내에 영원히 저장되지 않으며 필요에 따라 호출됨
```js
function del_document() {
    db.test.count()
}
del_document()
```
### 저장형
* 자주 실행되는 프로그램의 경우 데이터 베이스 내에 컴파일 된 실행 코드 저장 실행
```js
db.system.js.save({_id : "calculate_function", value : function(x, y, z){
      return (x + y) * z;
    } 
})
db.loadServerScripts();
// 함수 호출
calculate_function(1,1,1);
```

## Lock 정책
* 3.2 버전에는 Document Lock을 지원합니다. 
* MongoDB는 기본적으로 `Read Uncommitted`을 지원합니다. 
  * RDB는 기본적으로 `Read Committed`을 지원 

## document 제한 사항
### Document 크기 제한 
* BSON document크기 제한은 16MB
* document의 최대 크기는 단일 document가 과도한 양의 RAM을 사용하거나 과도한 대역폭을 사용할 수 없도록 합니다. 
* 최대크기보다 큰 문서를 저장하기 위해서는 GridFS API를 사용합니다. 
### Document 필드 순서 
* 아래의 예를 제외하고는 쓰기 작업의 순서를 유지합니다. 
  * _id는 항상 문서의 첫 번째 필드입니다.
  * 필드의 이름을 변경하는 업데이트로 필드의 순서가 변경 될 수 있습니다. 
### _id 필드
* 각 document들은 primary key의 역활을 하는 고유한 _id를 가집니다. 
* 삽입된 문서가 _id 필드를 생략하면 _id 필드에 대한 ObjectID를 자동으로 생성합니다.
#### _id 필드의 제약 조건
* 기본적으로 컬렉션을 생성할 때 _id필드에 대한 unique 인덱스를 생성합니다.
* _id 필드는 항상 문서의 첫 필드입니다. 
* 서버가 _id필드가 없는 문서를 수신하면 필드의 처음으로 이동합니다. 
* _id 필드에는 배열을 제외한 모든 BSON 데이터 유형의 값이 포함됩니다. 
  * replication 동작을 보장하려면 BSON 정규식 표현 타입을 _id 필드에 저장하지 마세요
* _id 값을 저장하기 위한 일반적인 옵션
  * ObjectID 사용
  * 가능하다면 자연 고유 식별자 사용, 공간의 절약 및 추가적인 index를 피합니다. 
  * 자동 증가 번호 생성
  * UUID를 어플리케이션에서 생성
    * 보다 효율적으로 생성하려면 UUID를 BSON binData 유형의 값으로 저장하세요
  * 