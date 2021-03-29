* 사이트 : https://docs.mongodb.com/manual/core/crud/

# MongoDB CRUD Concepts
* 이 섹션에는 MongoDB의 CRUD 작업과 관련된 개념 정보가 포함되어 있습니다.
## 원자성, 일관성 및 분산 작업 
### Atomicity and Transactions

#### Atomicity
* MongoDB안에서 Write 작업은 단일 document 수준에서 Atomic 합니다.
* 단일 문서 내의 포함된 여러 document들을 수정하는 경우에도 마찬가집니다. 
* 
#### 다중-Document Transactions
* 단일 write 작업(`예) updateMany()`)이 여러 문서를 수정하는 경우 각 문서의 수정은 원자적이지만 전체 작업은 atomic하지 않습니다.
* 다중-document write 작업을 수행할 때 단일 쓰기 작업이든 여러 쓰기 작업 모두 다 다른 작업들이 끼어들 수 있습니다. 
* MongoDB에서는 다중 document에 대한 쓰기와 읽기의 atomic한 상황이 필요할 때 다중 document 트랜젝션을 지원합니다.
  * 버전 `4.0`에서는 replica set에서 다중 문서 트랜젝션을 지원합니다. 
  * 버전 `4.2`에서는 분산된 클러스터에서 다중 document 트랜젝션을 지원하는 분산 트랜젝션을 도입하고 replica set에 지원하는 다중document에 대한 지원을 통합합니다. 
* 성능을 개선하기 위해서는 다중-Document를 최소한으로 사용하는 설계를 해야합니다. 

#### Concurrency Control(동시성 제어)
* Concurrency control 란 동시에 작용하는 다중 트랜젝션의 상호간섭 작용에서 DataBase를 보호하는 것을 말함
* 동시성 제어를 통해서 데이터 불일치 또는 충돌 없이 여러 어플리케이션을 동시에 실행 할 수 있습니다. 
  * 한가지 방법은 오직 unique한 값을 가질 수 있는 필드에 고유 인덱스를 만드는 것입니다. 
  * 이렇게 하면 삽입또는 업데이트로 중복 데이터가 생성되지 않습니다. 
  * 여러 필드에 unique 인덱스를 만들어서 필드 값 조합에 고유성을 적용합니다.
  * 또다른 방법은 쓰기 작업을 위한 쿼리 조건에서 필드의 예상값을 지정하는 것입니다. 

### Read Isolation, Consistency, and Recency
#### Isolation Guarantees(격리 보장)
* Read Uncommitted 
  * Read Concern에 따라서 클라이언트는 write가 durable 전에 write결과를 확인할 수 있습니다. 
    * `local` 또는 `available`를 read concern 가진 클라이언트
      * 발행한 클라이언트가 쓰기작업을 인증 받기 전에 write concern에 관계 없이 클라이언트는 쓰기 작업의 결과를 볼 수 있습니다. 
      * replica set의 failover로 롤백 될 수 있는 데이터를 읽을 수 있습니다. 
  * 다중-document 트랜잭션 작업이 커밋 되면 트랜젝션에 이루어진 데이터 변경을 트랜젝션 외부에서 볼 수 있습니다.
  * 트랜젝션이 커밋 될 때까지 트랜젝션의 데이터 변경사항은 외부에서 볼수 없습니다. 
  * 그러나 트랜젝션인 여러 shard에 쓰기 작업을할 때 커밋된 결과가 모든 샤드에 적용 될 때까지 외부의 읽기 작업들은 기다릴 필요가 없습니다. 
    * 예를 들어 `write 1`이 `shard A`에 커밋되었고 `write 2`가 `shared B`에 커밋되지 않더라도 read concern이 `local`인 외부 read `write1`에 접근 할 수 있습니다.
  * `Read Uncommitted`는 기본 격리 수준입니다.(standalone, replica set, shareded cluster)

#### Read Uncommitted And Single Document Atomicity
* 쓰기 작업은 단일 document에 대해서 원자적입니다. 
  * 예를 들어서 write가 여러 필드를 업데이트 하는 경우, 일부 필드만 업데이트 된 문서를 볼 수 없습니다. 
  * `read uncommitted`는 변경 작업이 durable 하기 전에 업데이트 된 문서를 볼 수 있습니다. 
* 단일 mongod 인스턴스를 사용하면 단일 문서에 대한 일련의 쓰기 및 읽기 작업을 직렬화 할 수 있습니다. 
* replica set을 사용하면 롤백이 없는 경우에만 read와 write 작업을 직렬화 할 수 있습니다. 

### Causal Consistency(인과 일관성)
* 작업이 논리적으로 이전 작업과 종속된 경우 작업 사이에 인과 관계가 있다고 합니다. 
  * 예를 들어 특정 조건에 의해서 모든 문서를 삭제하고 삭제 작업 확인하는 작업은 인과 관계가 있습니다.
* 인과 일관성이 있는 세션을 통해서 인과 관계가 지켜지는 순서로 작업을 실행해야 합니다. 

## Distributed Queries(분산 쿼리)
### Replica Set에 대한 읽기 작업 
*  

## 별첨
* Read Concern
  * Read Concern 이란 Replication에 몇개의 MongoDB 인스턴스가 해당 데이터를 가지고 있어야 데이터가 존재한다고 인정하는지에 대한 옵션
  * 문법
    * local : 본인 NODE 에만
    * majority : 다수의 NODE가 동의하면 read 성공
* Durable
  * 쓰기 작업이 하나 이상의 서버 프로세스가 종료하고 다시 시작해도 지속 될 수 있는 것을 의미합니다.
  * 단일 mongoDB서버의 경우 서버의 journal 파일에 기록된 경우를 의미합니다. 
  * replica set의 경우 쓰기 작업이 대부분의 투표할 수 있는 노드들에 지속되는 것을 의미합니다. 
