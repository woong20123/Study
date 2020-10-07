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

# 그래프형 데이터 모델 

#### 맵 리듀스에 대해서 조사하기

## 하둡 및 맵리듀스, 스파크
* https://3months.tistory.com/511