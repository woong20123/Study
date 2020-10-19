# 저장소와 검색 

## 데이터베이스를 강력하게 만드는 데이터 구조
* 많은 데이터 베이스는 내부적으로 append-only 데이터 파일인 `로그`를 사용함.
* 데이터 베이스에서 특정 키의 값을 효율적으로 찾기 위해서는 `인덱스`가 필요함.
  * 인덱스의 일반 개념은 부가적인 메타 데이터를 뜻함.
  * 인덱스는 검색속도를 향상 시키지만 반대로 쓰기 속도는 감소시킨다. 

## 해시 인덱스
* Append-only 로그형태를 사용하고 key-value로 해시를 사용해서 인덱스를 인메모리에 저장하는 구성 
  * key : 데이터 파일의 offset
  * Riak(Bitcast 엔진)

### 컴팩션
* append-only 형식으로 계속 데이터를 쌓게 된다면 디스크 공간이 부족해짐
* 이 상황을 회피하기 위해서 세그먼트 단위로 로그를 나눈 다음에 세그먼트 단위로 컴팩션을 실행합니다.
* 컴팩션이 로그에서 중복된 키를 버리고 최신값을 유지하는 것을 뜻함.

### 해시 인덱스의 단점 
* 해당 hash map의 크기가 메모리보다 커지게 되면 Swap 메모리가 발생되어 성능 저하가 발생  
  * 만약에 디스크에서 해시 맵을 유지 할 경우 랜덤 접근 I/O 발생
* 범위 질의에 비효율적이기 때문에 검색 범위가 일정크기 이상이면 fullscan이 더 효율적임
  
### Appendix

#### 카산드라에서 Row Tombstones
* Cassandra에서 삭제 요청을 받으면 실제로 데이터를 삭제하지 않고 tombstone이라고 기록을 합니다.
* tombstone표시된 데이터는 쿼리에 나타나지 않습니다.
#### tombstone 없이 삭제되는 예제
```sql
--값 A가 3개의 노드에 복제된 상태
[node1] [node2] [node3]
  [A]     [A]     [A]
           ↓
-- delete A 실행
-- node1,2에서만 삭제 성공, node3에서 실패
  []      []      [A]
           ↓
-- node3에 의해서 데이터가 복제됨
  [A]     [A]     [A]
```
#### tombstone으로 삭제되는 예제
```sql
--값 A가 3개의 노드에 복제된 상태 시작
     [node1]           [node2]           [node3]
       [A]               [A]               [A]
                          ↓
-- node1,2에서만 삭제 성공, node3에서 실패
[A, tombstone[A]]  [A, tombstone[A]]        []
           ↓
-- 삭제 요청이 실패해도 복제를 통해서 데이터 정합성 복구
[A, tombstone[A]]  [A, tombstone[A]]  [A, tombstone[A]] 
```
* 위의 예제에서 설명한 것 처럼 복제를 통한 복구 작업에도 삭제 작업이 문제없이 동작하도록 합니다. 
* 이것은 디스크 공간에 tombstone 표시가 영구적으로 저장된 다는 것을 의미합니다. 
* tombstone 표시를 영원히 유지하지 않으려면 gc_grace_seconds라는 매개변수를 사용합니다. 

#### gc_grace_seconds와 tombstone 제거 
* 테이블 단위의 gc_grace_seconds는 컴팩션을 통해서 제거하기 전에 tombstone된 데이터를  유지하는 기간을 제어합니다. 
* 이 값은 삭제 요청이 특정 노드에 실패에 대한 복구하는데 예상되는 시간을 반영 해야 합니다.
* gc_grace_seconds 경과하더라고 컴팩션이 발생되기 전까지 Tombstone은 삭제되지 않습니다. 
* 만약에 gc_grace_seconds 이상 동안 node가 연결이 끊겼고 컴팩션을 통해서 tombstone이 삭제되었다면 삭제된 데이터가 복구되는 현상이 발생 할 수 있습니다.
  * tombstone 없이 삭제되는 예제와 같은 독작
* gc_grace_seconds의 기본값은 10일입니다. 

#### 카산드라에서 Tombstones 종류
* Partition tombstones
* Row tombstones
* Range tombstones
* ComplexColumn tombstones
* Cell tombstones
* TTL tombstones

#### 참조 
  * https://cassandra.apache.org/doc/latest/operating/compaction/index.html?highlight=tombstone#tombstones-and-garbage-collection-gc-grace 
  * https://docs.datastax.com/en/dse/5.1/dse-arch/datastax_enterprise/dbInternals/archTombstones.html
#### 해시 알고리즘
* 해시 함수란 임의 길이의 데이터를 고정된 길이의 데이터로 매핑하는 함수 뜻함. 함수에 의해서 얻어지는 값을 해시라고 불름
* https://en.wikipedia.org/wiki/Hash_table > 번역 작업 필요
* 해시 충돌 해소


## SS테이블과 LSM트리