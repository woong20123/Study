# 데이터를 위한 질의 언어 
## 선언형 질의 언어와 명령형 코드
* 선언형 질의 언어에는 SQL이 있음
* 명령형 코드에는 IMS와 코다실이 있음
* 전체 동물들중에 상어만 반환한다면 코드를 질의로 표현한다면 
####  관계대수로는 아래와 같이 표현합니다.
```sql
sharks = σ family="sharks"(animals)
```  
#### SQL로 표현할 때는 관계 대수의 구조를 유사하게 따름
```sql
SELECT * FROM animals WHERE family = 'Sharks';
```

#### 명령형 코드는 특정 순서로 특정 연산을 수행하도록 지시함
```c++
void getSharks(std::vector<Shark> & sharks){
    for(int i = 0; i < animals.length;i++){
        if(animals[i].family == "Sharks")
            sharks.push_back(animals[i])
    }
}
```

### 선언형 질의 언어와 명령형 코드 차이점
|질의방식|특징|
|:--|:--|:--|
|선언형 질의 언어|조건만 지정하면 되며 내부적으로 쿼리 최적화기를 통해 수행<br>즉 간결하게 작업을 수행 가능 <br>상세 구현이 데이터베이스 엔진에 숨겨져 있음<br>조건이 명확하게 엔진에 전달되기 때문 병렬처리에 유리 |
|명령형 코드|수행방식을 직접 구현 해야 함<br>선언형보다 조건이 명확하게 지정하지 않아도 됨|
* 선언형 질의언어가 살아남은 것이 설명됨, 코드가 적으면서 최적화를 공통으로 관리한다.
  
### 웹에서의 선언형 질의
* 웹에서도 선언형 질의를 사용하게 되면 많은 이점이 있다.
```css
/* selected라는 클래스를 가진 li 엘리먼트에 백그라운드의 색을 blue로 변경 */
li.selected > p {
    background-color:blue;
}
```
* 위의 코드를 명령형 코드인 자바스크립트의 코어 DOM으로 표현한다면 매우 복잡해진다. 


## 맵리듀스(MapRedudce)
* MapRedudce란 많은 컴퓨터에서 대량의 데이터를 처리하기 위한 프로그래밍 모델로 구글에 의해서 알려짐
* 몽고DB와 카우치 DB를 포함한 일부 NoSQL에서 제한된 형태의 MapReduce를 지원합니다. 
* MapRedudce는 특이하게 명령형도 아니고 선언형도 아닌 중간에 위치함 
#### 한달에 상어를 자주 발견하는지에 대한 데이터 질의를 sql과 맵리듀스로 표현해봅니다. 
#### 예제 sql 문법
```sql
-- postgresql
SELECT date_trunc('month', observation_timestamp) AS observation_month,
sum(num_animals) AS total_animals
FROM observations
WHERE family = 'Sharks'
GROUP BY observation_month;
```
#### 예제 몽고 DB의 MapReduce 문법
```Map/Reduce
db.observations.mapReduce(
  function map() {
    var year = this.observationTimestamp.getFullYear();
    var manth = this.observationTimestamp.getMonth() + 1;
    emit(year + "-" + month, this.numAnimals);
   },
   function reduce(key, values){
     return Array.sum(values);
   },
   {
     query : { family : "Sharks"},
     out : "monthlySharkReport"
   }
);
```
#### 예제의 맵리듀스 동작 방식 
* map함수는 질의와 일치하는 모든 문서에 대해서 한번씩 호출 됩니다. 
* emit함수를 통해서 key/value형태의 값을 생성합니다. 
* 같은 key를 가지는 emit된 값들은 reduce함수를 호출합니다. 
* monthlySharkReport 컬렉션에 기록합니다. 

#### 몽고 DB의 맵리듀스 제한사항
* map과 reduce는 순수 함수이어야 한다.
* 대신 이 제한 사항을 통해서 임의의 순서로 실행 할 수 있고 장애가 발생해도 다시 실행 가능
  
#### 맵 리듀스이 사용성 문제
* 연계된 자바 스크립트 함수를 신중하게 작성해야 하는 문제 발생
  * 순서가 뒤바뀔수 있는 부분은 직접 제어한다.
* 하지만 선언형 질의는 쿼리 최적화기가 담당한다. 

## 그래프형 데이터 모델 
* 일대다 관계라면 문서 모델이 적합하고 단순한 다대다 관계라면 관계형 가능
* 복잡한 다대다 관계라면 그래프형이 적합
* 그래프 모델은 정점(vertex)과 간선(edge)으로 이루어짐
  * 소셜 그래프 - 정점은 사람이고 간선은 관계를 나타냄
  * 웹 그래프 - 정점은 웹 페이지고 간선은 HTML 링크를 나타냄
  * 도로 네트워크 - 정점은 교차로 간선은 도로선을 나타냄 
* 그래프 모델에서 동작하는 알고리즘
  * 자동차 네비게이션 
  * 페이지 랭크
* 그래프의 정점은 다양한 유형의 데이터를 저장할 수 있음
  * 페이스북에서는 정점에 사람, 장소, 이벤트, 체크인, 코멘트를 나타냄

### 속성 그래프
#### 정점의 요소
* 고유한 식별자
* 유출(outgoing) 간선 집합
* 유입(incoming) 간선 집합
* 속성 컬렉션(키-값 쌍)
#### 간선의 요소
* 고유한 식별자
* 간선이 시작하는 정점
* 간선이 끝나는 정점
* 두 정점간의 관계 유형 설명
* 속성 컬렉션 
  
### SQL로 표현한 속성 그래프
```sql
-- 정점은 고유한 식별와 json 형태의 속성 정보를 가짐
-- 속성에는 유출, 유입 간선 집합, 속성 컬랙션 정보가 저장됨
CREATE TABLE vertices(
  vertex_id integer PRIMARY KEY,
  properties json
);

-- 간선은 고유한 식별와 연결된 정점의 정보 및 레이블, json 형태의 속성 정보를 가짐
-- 간선의 컬럼이 많은 이유는 조회를 위해서 필요한 정보이기 때문입니다. 
CREATE TABLE edges(
  edge_id integer PRIMARY KEY,
  tail_vertex integer REFERENCES vertices (vertex_id),
  head_vertex integer REFERENCES vertices (vertex_id),
  label text,
  properties json
);

CREATE INDEX edges_tails ON edges (tail_vertex);
CREATE INDEX edges_heads ON edges (head_vertex);
```
### 속성 그래프의 중요점
* 정점들은 간선으로 연결되며 정점의 유형에 대한 연결에 대한 제한이 없다
* 정점이 주어지면 유출, 유입 간선을 효율적으로 검색 가능하고 그래프를 순회할 수 있음
  * 앞 뒤로 순회 가능 
* 서로 다른 스키마 구조를 저장하면서 깔끔하게 유지 할 수 있음
  * 확장성이 좋음

### 사이퍼 질의 언어
* 사이퍼는 속성 그래프를 위한 선언형 질의 언어이고 neo4j를 위해서 만들어 짐
### 사이퍼 질의 예제
#### 사이퍼를 사용해서 그래프 모델 선언
```sql
-- Lucy라는 사람에 대한 그래프 정보
CREATE
  (NAmerica:Location  {name:'North America', type:'continent'}),
  (USA:Location       {name:'United States', type:"country"}),
  (Idaho:Location     {name:'Idaho', type:'state'}),
  (Lucy:Person        {name:'Lucy'}),
  (Idaho) -[:WITHIN]->(USA)-[:WITHIN]-> (NAmerica),
  (Lucy) -[:BORN_IN]-> (Idaho)
```
#### 사이퍼를 사용해서 크래프 모델 질의
```sql
-- WITHIN*0는 WITHIN를 0회 이상 따라가라는 뜻
MATCH
  (porson) -[:BORN_IN]-> () -[:WITHIN*0..]-> (us:Location {name:'United Stateds'})
  (person) -[:LIVES_IN]-> () -[:WITHIN*0..]-> (eu:Location {name:'Europe'})
RETURN person.name
```
#### 위의 조건식은 미국에서 태어나서 유럽으로 이민온 사람을 검색하는 쿼리
  * BORN_IN 간선을 따라서 Location이 United Stateds에 도달할 때까지 따라갑니다. 
  * LIVES_IN 간선을 따라서 Location이 Europe에 도달할 때까지 따라갑니다. 
* 만약에 name에 인덱스가 있다면 'United Stateds', 'Europe' 정점을 효율적으로 찾아서 역순으로 검색 할 수 있습니다. 

#### 만약에 위의 예제를 SQL로 표현하게 된다면 매우 복잡해진다.

### 트리플 저장소와 스파클
* 트리플 저장소는 모든 정보를 형식(subject(주어), predicate(서술어), object(목적어))으로 저장합니다.
* 트리플에서 주어는 그래프의 정점을 뜻합니다. 
* 목적어는 다음의 두가지 중 하나 입니다.
  * 문자열이나 숫자가 같은 데이터 타입 값
    * 속성 값을 의미 합니다.
  * 그래프의 다른 정점
* 서술어는 목적어가 데이터라면 속성의 키값, 정점이라면 간선이 됨

#### 트리플 예제
```sql
-- 주어 서술어  목적어
@prifix
_:lucy  a       :Person.
_:lucy  :name   "Lucy".
_:lucy  :bornIn _:idaho.
_:idaho a       :Location
_:idaho :name   "Idaho".
_:idaho :type   "State".
_:idaho :within _:usa.
```
* 그래프의 정점은 `_:정점이름`으로 표현합니다.
 * 그래프의 정점은 `_:정점이름`으로 표현합니다.

### 시맨틱 웹
* 시맨틱 웹이란 기존에 작성된 HTML문서는 컴퓨터보다는 사람이 읽을 수 있는 정보로 되어 있으니 기계(컴퓨터)가 이해할 수 있는 형태로 표현하기 위한 프레임 워크
* RDF(Resource Description Framework)는 서로 다른 웹사이트가 일관된 형식으로 데이터를 게시하기 위한 방법을 제안함
* 간단한 예시 가장 간단한 RDF인 트리플로 HTML을 표현
```html
<em>바나나</em>는 <em>노란색</em>이다

<urn:바나나, urn:색, urn:노랑>
```

### 스파클 질의 언어
* 스파클은 RDF 데이터 모델을 사용한 트리플 저장소 질의 언어임 
```SQL
PREFIX : <urn:example>

SELECT ?personName WHERE {
  ?person :name ?personName.
  ?person :bornIn / :within* / :name "United States".
  ?person :livesIn / :within* / :name "Europe".
}
```

## 정리
* 관계형, 문서형, 그래프 모델 모두 다 사용되고 있으며 특성에 맞도록 사용해야함
  * 관계형 : 역사적으로 트리 구조로 표현하지 못한 다대다를 표현하기 위해 사용
    * 
  * 문서형 : 데이터가 문서 자체에 포함되어 있음, 문서간의 관계가 거의 없는 경우 사용
  * 그래프형 : 모든 것이 잠재적으로 관련 있다는 추정하에 사용
* 문서형, 그래프형은 스키마의 강제하지 않음(읽기 스키마 구조)
* 각 데이터 모델은 각자의 쿼리언어나 프레임 워크 제공함
  * sql, 맵리듀스, 몽고DB 집계 파이프라인, 사이퍼, 스파클, 데이터로그 
  
## Append
### neo4j 설치 하기 
* https://neo4j.com/developer/docker-run-neo4j/
#### 맵 리듀스에 대해서 조사하기

## 하둡 및 맵리듀스, 스파크
* https://3months.tistory.com/511