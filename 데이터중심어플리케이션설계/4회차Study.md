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

#### 카프카의 데이터 로그 저장 방식이 해시 색인 방식을 사용합니다. 
  
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

#### 참조 
  * https://cassandra.apache.org/doc/latest/operating/compaction/index.html?highlight=tombstone#tombstones-and-garbage-collection-gc-grace 
  * https://docs.datastax.com/en/dse/5.1/dse-arch/datastax_enterprise/dbInternals/archTombstones.html

#### 해시 알고리즘
* 해시 함수란 임의 길이의 데이터를 고정된 길이의 데이터로 매핑하는 함수 뜻함. 함수에 의해서 얻어지는 값을 해시라고 불름
* 해시 충돌 해소 방법


## SS테이블과 LSM트리
### SS테이블(Sorted String Table)
* 로그 구조화 저장소의 세그먼트의 데이터를 키 값을 기준으로 정렬해서 관리하는 데이터 구조
### SS테이블 장점
* 세그먼트에 내의 데이터가 정렬되어 있기 때문에 부분 데이터를 읽고 병합이 가능함
* 특정 키를 검색하기 위해서 모든 키의 인덱스를 메모리에 유지 할 필요 없음
  * 정렬되어 있기 때문에 드문드문 인덱스를 생성할 수 있음
* 희소 인덱스의 구간 만큼 블록을 그룹화해서 단위별로 압축 가능 
  
### LSM 트리 데이터 관리 방법
* 데이터 생성 흐름
  * InputData -> 인메모리(memtable) -> (일정 크기에 도달) -> 디스크(SSTable)
* memtable이란 인메모리에 유지되는 밸런스 트리 구조(B tree)
* 장애발생시 memtable 데이터가 유실을 방지하기 위해서 로그를 따로 유지 해야 함

### LSM 트리(Log-Structured Merge-Tree) 
* memtable(`레벨0`, 인메모리)과 SSTable(`레벨1`, 디스크)을 사용해서 구조화 된 데이터 구조 
* 검색방식
  * memtable 검색 -> SSTable중에 최신 세그먼트부터 검색
  * 검색 키가 없는 경우 검색에 오랜 시간 소요
  * 해당 기능을 최적화하기 위해서 `블룸 필터` 사용
* 병합(merge) 방식
  * size-tiered
    * https://cassandra.apache.org/doc/latest/operating/compaction/stcs.html#stcs
  * leveled compaction
    * https://cassandra.apache.org/doc/latest/operating/compaction/lcs.html#lcs
  * 카산드라는 두가지 모두 지원


### Appendix
#### 블룸 필터란
* 해시 함수와 n개의 비트형태의 데이터 구조 사용하는 확률적 자료 구조
* 특징은 긍정 오류는 발생하지만 부정오류는 발생 하지 않음을 보장함
* 데이터 처리 방식
  * 원소 추가
    * 원소를 해시함수로 처리해서 나온 값의 비트를 킨다.
  * 원소 검사
    * 원소를 해시함수로 변환한 값의 비트를 검사. 1이면 포함, 0이면 없음
* 긍정 오류가 발생하는 이유
  * 해시 충돌로 인해서 발생함

#### 긍정오류와 부정오류
|이름|필터|실제|
|:--|:--|:--|
|긍정오류(false positive |집합에 속함|집합에 속하지 않음|
|부정오류(false negative)|집합에 속하지 않음|집합에 속함|

#### 카산드라의 블룸 필터 (번역, 요약 필요)
* 카산드라는 Ram에 있는 데이터를 디스크에 병합합니다. 
* 요청되는 파티션의 모든 SSTable 풀 스캔을 방지하기 위해서 블룸 필터라는 데이터구조 사용
* 블룸필터는 카산드라가 2가지 상태중에 하나인지 결정할 수 있는 확률적 데이터 구조입니다.
  * 파일에 데이터가 확실이 존재하지 않는다.
  * 데이터가 파일에 있을 수도 있음
* 블룸필터는 주어진 SSTable에 데이터가 존재한다고 보장할 수 없지만 더 많은 RAM을 사용하게 하면 조금 더 정확하게 만들 수 있습니다 
* 테이블 별로 bloom_filter_fp_chance 값을 0 ~ 1사이의 소수점으로 조절할 수 있습니다. 
* bloom_filter_fp_chance 기본값은 0.01이고 LeveledCompactionStrategy은 0.1입니다.
* 블룸 필터는 램에 저장되고 offHeap에 저장되기 때문에 운영자는 최대 힙크기 블룸필터 크기를 고려하지 않아도 됩니다. 
* 정확도가 향상 됨으로써 메모리 사용량이 비선형적으로 증가합니다. (0에 가까워질수록 정확도 증가)
  * bloom_filter_fp_chance인 0.01인 경우는 0.1보다 3배의 메모리를 필요로 합니다. 
* 카산드라의 인스턴스 장비의 램과 디스크의 상태에 따라서 조절해야합니다. 
  * 램이 넉넉하고 디스크를 아껴야 한다면 bloom_filter_fp_chance을 낮게 유지

#### 참조 사이트
* https://meetup.toast.com/posts/192
* https://cassandra.apache.org/doc/latest/operating/bloom_filters.html?highlight=bloom%20filter

## B-Tree