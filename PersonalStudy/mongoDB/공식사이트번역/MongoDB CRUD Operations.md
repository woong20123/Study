# 사이트 링크 : https://docs.mongodb.com/manual/crud/

# Create Operation
* collection에 새로운 document를 추가합니다. 
## db.collection.insertOne()
* 예제 문법
```js
db.collection.insertOne(
   <document>,
   {
      writeConcern: <document>
   }
)
```
* document 
  * collection 추가할 문서 형식
* writeConcern 
  * 선택, 기본쓰기를 사용하려면 생략합니다.
  * 트랜젝션에 실행 되는 경우 writeConcern 명시하지 않습니다.
  * https://docs.mongodb.com/manual/reference/write-concern/ 
  * w : 쓰기 작업의 인증에 대한 설정 값 지정
  * j : 
  * wtimeout : 쓰기작업에 대한 타임아웃 값 지정

## db.collection.insertMany
* 예제 문법
```js
db.collection.insertMany(
   [ <document 1> , <document 2>, ... ],
   {
      writeConcern: <document>,
      ordered: <boolean>
   }
)
```
* ordered
  * 선택, 정렬된 insert를 수행하는지 여부 

# Read Operations
* collection에서 document를 검색합니다. 
## db.collection.find()
* 예제 문법
```js
db.collection.find(query, projection)
// project info
// <field> : <1 or true> => find 결과에 포함
// <field> : <0 or false> => find 결과에 포함
```
* query
  * Optional. 쿼리 연산자를 사용해서 필터를 지정합니다. 
  * 모든 문서를 검색하려면 매개변수를 생략하거나 `{}`를 사용하세요
* projection
  * Optional. 반환할 필드를 지정합니다. 

# Update Operations
* collection에 document를 업데이트 합니다.
## db.collection.updateOne()
* 예제 문법 
```js
db.collection.updateOne(
   <filter>,    
   <update>,
   {
     upsert: <boolean>,              // 
     writeConcern: <document>,       // 요청에 대한 응답 방식 위에서 설명함
     collation: <document>,          // 문자에 대해서 설정 함
     arrayFilters: [ <filterdocument1>, ... ],
     hint:  <document|string>        // Available starting in MongoDB 4.2.1
   }
)
```
* upsert
  * optional. trun이면 updateOne() 
  * 필터와 일치하는 문서가 없으면 새문서를 만듭니다. 
  * 필터와 일치하면 문서를 업데이트 합니다. 
  * 기본값 false
## db.collection.updateMany()
* 예제 문법 
```js
db.collection.updateMany(
   <filter>,
   <update>,
   {
     upsert: <boolean>,
     writeConcern: <document>,
     collation: <document>,
     arrayFilters: [ <filterdocument1>, ... ],
     hint:  <document|string>        // Available starting in MongoDB 4.2.1
   }
)
```
## db.collection.replaceOne()
* 필터를 기반으로 collection 내의 단일 문서를 변경합니다. 
* 예제 문법 
```js
db.collection.replaceOne(
   <filter>,
   <replacement>,
   {
     upsert: <boolean>,
     writeConcern: <document>,
     collation: <document>,
     hint: <document|string>                   // Available starting in 4.2.1
   }
)
```

# Delete Operations
## db.collection.deleteOne()
* collection의 단일 문서를 제거합니다. 
* 예제 문법
```js
db.collection.deleteOne(
   <filter>,
   {
      writeConcern: <document>,
      collation: <document>,
      hint: <document|string>        // Available starting in MongoDB 4.4
   }
)
```
## db.collection.deleteMany() 
* collection의 filter된 모든 문서를 제거합니다. 
* 예제 문법
```js
db.collection.deleteMany(
   <filter>,
   {
      writeConcern: <document>,
      collation: <document>
   }
)
```
# Bulk Write
* https://docs.mongodb.com/manual/core/bulk-write-operations/

