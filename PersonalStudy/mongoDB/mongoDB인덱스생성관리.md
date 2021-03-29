## 인덱스 생성 문법
```js
db.emp.createIndex({eno : 1, }, {unique : true}) // 1(Asc), -1 (Desc)
db.emp.createIndex({job : -1})

db.emp.getIndexes()

db.emp.dropIndexes()
db.emp.dropIndex({eno : -1})
```

## 인덱스 재구성과 삭제
* MongoDB에서 제공하는 B-Tree는 빠른 검색이 제공되지만 입력과 삭제가 빈번한 경우 불균형이 발생 가능
* 이 문제를 해결하려면 인덱스를 재구성해서 해결 할 수 있습니다. 
```js
db.emp.createIndex({eno : 1}, {unique : true})
db.emp.reIndex()
```

### 인덱스를 사용할 때 주의점
* 충분한 EXTENT 공간이 있지 않다면 더 큰 EXTENT 공간으로 마이그레이션이 발생하여 성능저하가 발생할 수 있음
* SORT(), LIMIT() 절을 적절하게 사용하는 것이 불 필요한 데이터 검색을 피할 수 있으므로 성능향상에 도움이 됩니다. 

## 인덱스 종류
### Single-Key 인덱스, Compound key 인덱스
* 키를 여러 필드로 지정할 수 있습니다. 
### Non-Unique 인덱스, Unique 인덱스 
* 중복 여부를 지정할 수 있습니다.
```js
// unique 인덱스 생성
db.employees.createIndex({ empno : 1}, { unique : true})

// Nom Unique 인덱스 생성 
db.employees.createIndex({ ename : 1})
```
### Sparse 인덱스 
* Sparse 인덱스는 검색 대상 필드의 값이 전체 컬렉션에서 차지하는 밀도가 낮은 경우 유리합니다. 