## 인덱스 
#### 인덱스는 MongoDB에서 쿼리를 효율적인 실행을 지원합니다. 
#### 인덱스가 없으면 MongoDB는 쿼리문과 일치하는 문서를 선택하기 위해서 컬렉션 스캔을 수행합니다
* 컬랙션 스캔은 모든 문서를 스캔함
#### 쿼리에 적합한 인덱스가 있는 경우 MongoDB는 인덱스를 사용해서 검사해야 하는 문서 수를 제한 할 수 있습니다.
#### 인덱스는 컬렉션 데이터 셋트의 작은 부분을 탐색하기 쉬운 형식으로 저장하는 특수 데이터 구조입니다. 
#### 인덱스는 특정 필드 또느 필드 셋트 값을 필드에 값을 정렬하여 저장합니다. 
#### 정렬된 인덱스 순서는 범위 기반 및 일치하는 값을 찾는 작업을 지원합니다. 
#### 또한 MongoDB는 인덱스 순서를 사용해서 정렬된 결과를 반환할 수 있습니다.
#### 다음 다이어그램은 인덱스를 사용하는 일치하는 문서를 선택하고 정렬하는 쿼리를 보여줍니다. 
#### 기본적으로 MongoDB의 인덱스는 다른 데이터 베이스 시스템의 인덱스와 유사합니다. 
#### MongoDB은 컬렉션 수준에서 인덱스를 정의하고 MongoDB 컬렉션이 있는 문서의 모든 필드 또는 하위 필드에 대한 인덱스를 지원합니다.
#### MongoDB는 컬랙션이 생성 될 때 _id 필드에 unique_index를 생성합니다. 
#### _id 인덱스는 클라이언트가 _id 필드에 대해서 중복된 값을 가지는 문서를 방지합니다. 
#### 사용자는 _id필드의 인덱스를 제거할 수 없습니다. 
* 샤딩된 클러스터에서 _id 필드를 샤드키로 사용하지 않는 경우 애플리케이션인 오류를 방지하기 위해서 _id필드 값의 고유성을 확인해야합니다. 
* 이는 대부분의 경우 표준 자동 생성 ObjectID를 사용하여 수행됩니다.
#### 몽코에서 인덱스를 생헝하는 방법
```sql
db.collection.createIndex( { name: -1 } )
```
* MongoDB 인덱스는 B-tree 구조를 사용합니다.

## Index 이름
* 인덱스의 기본 이름은 인덱스화 된 키와 정렬 방향을 연결한 값을 가집니다.(1, -1은 정렬 방향)
* 예를 들어서 {item : 1, quantity : -1} 인덱스를 생성하면 item_1_quantity_-1 이름을 가집니다. 

## Index 타입
* MongoDb는 특정 유형의 데이터 및 쿼리를 지원하기 위해 다양한 인덱스 유형을 제공합니다. 
### Single Field
* MongoDB는 _id외에도 Document의 단일 필드에서 사용자 오름차순/내림차순 인덱스 생성을 지원합니다. 
```sql
{ score: 1}
score를 키로 내림차순으로 정렬해라
```
* 단일 필드 인덱스 및 정렬 작업의 경우 MongoDB가 인덱스를 양방향으로 탐색할 수 있으므로 인덱스 키의 정렬 순서는 중요하지 않습니다. 

### Compound Index
* MongoDB는 또한 여러 필드, 즉 복합 인덱스에 대한 사용자 정의 인덱스를 지원합니다. 
* 복합 색인에 나열된 필드의 순서는 중요합니다. 
* 복합 인덱스가 {userid : 1, score : -1}로 구성된경우 인덱스는 먼저 userid 별로 정렬된 다음 score별로 정렬됩니다. 
* MYSQL의 복합 인덱스와 구조가 동일함
* 복합 인덱스 및 정렬 작업의 경우 인덱스 키의 정렬 순서에 따라서 인덱스 정렬 작업을 지원할 수 있는지 여부를 결정합니다. 
  
### Multikey Index 
* MongoDB는 다중 키 인덱스를 사용해서 **배열**에 저장된 콘텐츠를 인덱싱합니다. 
* 배열 값이 있는 필드를 인덱싱하면 MongoDB는 배열의 모든 요소에 대해 별도의 인덱스 항목을 만듭니다.
* 이러한 멀티키 인덱스를 사용하면 배열의 요소나 요소에 매칭된 배열이 포함된 문서를 선택할 수 있습니다. 
* 인덱싱된 필드에 배열 값이 포함 된 경우 멀티 키 인덱스를 자동으로 생성합니다. 
* Multikey Index는 동시에 같은 document를 가르킬수 있기 때문에 shared key로 사용 불가
* https://sarc.io/index.php/nosql/1735-mongodb-index-multi-key-index

* 예제 인덱스 생성
```java
db.user.insert({
name:"kimdubi",
addr:{ city : "seongnam", dong: "sampyeong" },
db:["MongoDB","Mysql","Oracle"]})

// embeded document field
db.user.createIndex({addr:1})
// array field
db.user.createIndex({db:1})
```
* 예제용 검색용
```java 
// 인덱스 정의문 그대로 검색 가능
db.user.find({addr:{city:"seongnam",dong:"sampyeong"}}).pretty()

// 인덱스 정의문에서 일부를 제거하면 검색불가
db.user.find({addr:{dong:"sampyeong"}}).pretty()
db.user.find({addr:{city:"seongnam"}}).pretty()

// embeded document의 field 지정 검색 가능
// embeded document의 field별로 인덱스가 생성됨
db.user.find({"addr.city":"seongnam"})
db.user.find({"addr.dong":"sampyeong"})

// array의 경우 검색가능 
db.user.find({db:"MongoDB"})

```



#### Index Bounds
* 만약 인덱스가 multikey인 경우 인덱스 경계 계산은 특수한 규칙을 따릅니다. 

### Geospatial Index
* MongoDB는 지리 공간 좌표 데이터를 효율적으로 지원 하기 위해서 2가지 타입의 인덱스를 지원합니다. 
* 2d indexes와 2dsphere indexes

### Text Indexes
* 전문처리?
### Hashed Indexes
* hash 기반 샤딩을 위해서 필드 값의 해시를 인덱스를 지원합니다. 
* 일반적인 해시와 마찬가지로 일치 값에 대해서는 조회가 가능하지만 범위값을 조회할 수 
없습니다.
### Hashed Sharding
* 해시 샤딩은 단일 필드나 복합 해시 인덱스(4.4버전 신규 기능)를 샤드 키로 사용해서 클러스터에 데이터를 분할 합니다.
* 이건 이번 스터디의 범위를 넘어간다.!! 카산드라랑 비슷하게 샤딩하넹

## Index 속성

### Unique index
* unique 속성으로 MongoDB는 인덱싱 된 필드에 대한 중복값을 거부합니다. 
* unique 속성외에도 unique 인덱스는 다른 MongoDB 인덱스와 기능적으로 상호 교환 할 수 있습니다.

### Partial index
* Partial index는 지정된 필터 표현식을 충족하는 컬렉션의 문서만 인덱싱합니다. 
* 컬렉션에 있는 문서의 하위 집합을 인덱싱 해서 Partial index는 인덱스 생성가 유지관리를 위한 저장공간 및 성능 비용을 효율적으로 관리합니다. 
* Partial index는 sparse index의 기능적으로 상위에 있으므로 더 선호되어야 합니다. 
* 예를 들면 다음 예제는 rating 필드가 5보다 큰 document만 인덱싱하는 복합 인덱스를 만듭니다. 
* MongoDB는 쿼리나 sort 작업이 불완전한 결과가 발생한다면 Partial index를 사용하지 않습니다.
* Partial index를 사용하려면 쿼리 조건의 일부로 필터 표현식이 포함되어야 합니다. 
```java
db.restaurants.createIndex(
   { cuisine: 1 },
   { partialFilterExpression: { rating: { $gt: 5 } } }
)

// 사용가능 부분 인덱스 범위에 들어 있음 - rating이 8보다 크거나 같음
db.restaurants.find( { cuisine: "Italian", rating: { $gte: 8 } } )

// 사용불가 부분 인덱스 범위 아님 - rating이 8보다 작음
db.restaurants.find( { cuisine: "Italian", rating: { $lt: 8 } } )

// 부분 인덱스의 범위조건이 없음 불가 !!
db.restaurants.find( { cuisine: "Italian" } )
```

### Sparse Indexes
* Sparse index는 인덱스된 필드를 가진 문서에 대한 항목만 포함합니다. 해당 필드가 널 값이어도 포함됨
* 이 인덱스는 인덱스된 필드가 없는 문서는 건너 뜁니다. 
* 이 인덱스는 모든 문서를 포함하지 않기 때문에 sparse입니다.
* 반대로 non-Sparse 인덱스는 collection안에 모든 문서를 가지고 인덱스된 필드를 포함하지 않는 문서에 대해서는 Null으로 지정합니다. 
* 3.2버전 부터는 partial indexes을 생성할 수 있습니다. 
  * partial indexes는 Sparse index보다 상위 집합을 제공하기 때문에 더 선호되어야 합니다. 

### TTL indexes
* TTL index는 MongoDB가 특정 시간 후에 컬렉션에 문서를 자동으로 제거하는 데 사용 할 수 있는 특수 단일 필드 인덱스 입니다. 
* 데이터 만료는 제한된 시간동안만 데이터 베이스에 유지 되어야 하는 머신 생성 이벤트 데이터, 로그 및 세션 정보와 같은 특정 유형의 정보에 유용합니다. 
* 인덱스된 필드의 값의 지정된 expireAfterSeconds값을 더한 시간이 지난후에 document는 만료됩니다.
* mongod의 백그라운드 스레드를 통해서 만료된 문서는 제거됩니다. 
```java
db.eventlog.createIndex( { "lastModifiedDate": 1 }, { expireAfterSeconds: 3600 } )
```
  
#### TTL indexes 비고
* 스토리지 비용을 절약하기 위해서 Document를 제거하는 경우 MongoDB Atlas의  Online Archive를 고려하세요

### Hidden Index

## 참조 
* https://docs.mongodb.com/manual/indexes/
