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

#### `카프카의 데이터 로그` 저장 방식이 해시 색인 방식을 사용합니다. 
  
### Appendix
#### 카산드라에서 Row Tombstones
* Cassandra에서 삭제 요청을 받으면 실제로 데이터를 삭제하지 않고 tombstone이라고 기록을 하며 tombstone표시된 데이터는 쿼리에 나타나지 않습니다.
#### tombstone을 사용하는 이유
* 컴팩션시 한번에 데이터를 지워서 효율적인 측면도 있다.
  * 대신에 컴팩션이 진행될 때 까지 쓸모없는 데이터가 유지된다. 
* 아래 예제처럼 노드간의 데이터의 안정성을 보장하기 위함도 있음
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

#### gc_grace_seconds와 tombstone 제거 
* tombstone은 얼마나 유지되어야 할까요?
* 카산드라에서는 테이블 단위로 gc_grace_seconds값으로 유지기간을 제어합니다. 
* 이 값은 위에서 설명한 특정 노드의 삭제 실패에 대한 복구하는데 예상되는 시간이 반영되어함
* gc_grace_seconds의 기본값은 10일

#### 참조 
  * https://cassandra.apache.org/doc/latest/operating/compaction/index.html?highlight=tombstone#tombstones-and-garbage-collection-gc-grace 
  * https://docs.datastax.com/en/dse/5.1/dse-arch/datastax_enterprise/dbInternals/archTombstones.html

#### 해시 알고리즘
* 해시 함수란 임의 길이의 데이터를 고정된 길이의 데이터로 매핑하는 함수 뜻함. 
* 함수에 의해서 얻어지는 값을 해시라고 불름
* 해시 충돌 해소 방법
  * Open addressing(linear probing)
    * 버킷당 들어갈수 있는 엔트리가 하나뿐인 해시 테이블
    * 충돌 발생시 비어있는 공간을 찾아서 넣어줍니다. 
  * Separate Chaining
    * 충돌 발생시 첫부분을 링크드 리스트로 구성함
  * 데이터가 적다면 Open addressing가 유리하지만 데이터가 많다면 Separete Chaning 유리함
* Java8에서의 개선된 해시 충돌 개선
  * 충돌된 데이터가 8개가 모이면 링크드 리스트(레드블랙트리)로 변경
  * 다시 해당 값이 6개가 되면 링크드 리스트(레드블랙트리)로 변경

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
* 요청되는 파티션의 모든 SSTable 풀 스캔을 방지하기 위해서 블룸 필터라는 데이터구조 사용
* 블룸필터는 카산드라가 2가지 상태중에 하나인지 결정할 수 있는 확률적 데이터 구조입니다.
  * 긍정오류만 발생
    * 필터에서 존재하지 않으면 데이터는 존재하지 않는다.
    * 필터에서 존재한다고 해서 데이터가 존재한다고 100% 가정 불가
* 카산드라에서는 `bloom_filter_fp_chance`(0 ~ 1) 변수를 통해서 블룸필터의 정확도를 조절 할 수 있음
  * `bloom_filter_fp_chance` 낮을 수록 정확도 증가
  * 정확도가 높을 수록 Ram소비 증가
  * `bloom_filter_fp_chance`인 0.01(오차율 1%)인 경우는 0.1(오차율 10%)보다 3배의 메모리를 필요로 합니다. 
* 블룸 필터는 램에 저장되고 offHeap에 저장되기 때문에 운영자는 최대 힙크기 블룸필터 크기를 고려하지 않아도 됨

## Compaction Strategy
#### SizeTieredCompactionStrategy 특징
* 비슷한 크기의 sstable들을 병합하도록 구현됨 
  * 구현 난이도 낮음
* 데이터가 커짐에 따라 발생되는 문제
  1. 첫번째
     * row가 저장되는 sstable이 지정되지 않기 때문에 성능이 일관되지 않음
     * 최악의 경우 모든 sstable에 조회하려는 row가 있을 수 있습니다.
     
  2. 두번째 
     * 컴팩션의 특성상 빠른 병합이 보장되지 않기 때문에 공간 낭비 발생
     * 특히 삭제 비율이 높을 때 발생함
  3. 세번째
      * 반복되는 컴팩션으로 sstable이 커지면 병합시 사용 공간이 문제가 될 수 있음
      * sstable의 병합을 하기 위해서는 컴팩션되는 sstable의 100% 여유공간이 필요함

#### LeveledCompactionStrategy 특징
* sstable들을 레벨 단위로 나뉘어서 관리하며 sstable 작은 단위 크기로 고정됨
* 같은 레벨의 sstable의 내용이 겹치지 않음을 보장함
* 해당 레벨이 가득차면 다음레벨로 확장되며 다음레벨은 이전 레벨의 10배(기본값)임
* 컴팩션 작업은 겹치는 하위레벨의 모든 sstable을 포함해서 수행됨
* tiered compaction의 문제 해결
    * leveled compaction은 모든 읽기의 90%가 sstable에서 충족되도록 보장합니다.(거의 균일한 행 크기 가정)
      * 최악의 경우는 총 레벨에 의해서 제한됨
        * 예를 들어 10TB는 7레벨
    * 사용되지 않는 행으로 인해 최대 10% 공간이 낭비됩니다.
    * 컴팩션을 위한 임시 공간이 테이블의 크기의 10배의 공간만 예약하면 됩니다.

### 컴팩션 관련 번역 
#### https://cassandra.apache.org/doc/latest/operating/compaction/lcs.html#lcs
* LCS의 기본 아이디어는 모든 sstable들이 다른 level에 배치하여 같은 level안에 sstable이 겹치지 않음을 보장합니다.
* 겹침이란 단일 sstable의 first/last 토큰이 다른 sstable과 겹치지 않음을 의미합니다. 
* 즉 SELECT의 경우 레벨당 하나의 sstable 안에서 파티션 키를 찾아야 합니다.
* 각 레벨은 이전 것보다 10배이고 각 sstable은 기본적으로 160MB입니다. 
* L0은 sstable이 streamed/flushed되고 중복 없음이 제공됩니다. 
* compaction 후보를 선택 할 때 compaction은 대상 level에서 겹침이 발생되지 않도록 합니다. 
* 다음 레벨에서 모든 겹치는 sstable을 포함해서 수행됩니다. 
* 예를 들어 L3에서 sstable을 선택하면 L4에서 겹치는 모든 sstable을 선택하도록 보장해야 하고 compaction을 시작하면 진행중인 compaction이 겹침을 생성하지 않도록 해야 합니다.
* 겹침을 만들지 않을 것이라고 보장하면 레벨 안에서 병렬로 compaction을 시작할 수 있습니다. 
* L0 -> L1 compaction의 경우 L0 sstable은 전체 범위를 포함하기에 대부분 모든 L1 sstable을 포함해야합니다. 
* 너무 많은 메모리를 사용하기 때문에 하나의 compaction안에서 모든 L1 sstable과 모든 L0 sstable을 compact할 수 없습니다. 

#### https://cassandra.apache.org/doc/latest/operating/compaction/stcs.html#stcs
* SizeTieredCompactionStrategy (STCS)의 기본 아이디어는 동일한 크기의 안정된 데이터를 병합하는 것입니다. 
* 모든 SStable은 크기에 따라서 서로 다른 버킷에 배치됩니다. 
* 버킷안에 있는 sstable들의 평균 크기가 bucket_low와 bucket_high 사이라면 sstable이 버킷에 추가됩니다. 
* 여러개의 버킷들이 생성되고 가장 interesting 한 버킷들은 컴팩션 될 것입니다. 
* interesting 한것에 대한 판정은 bucket의 sstable들 중에 가장 많이 read를 하는 것을 파악하는 것입니다. 
* Major compaction
* STCS로 major 컴팩션이 수행 될 때 데이터 디렉토리당 두개의 sstable들이 생성됩니다. 
  * (하나는 복구된 데이터이고 하나는 복구되지 않은 데이터용)
* STCS options
* min_sstable_size (기본값 : 50MB)
  * 이값보다 sstable이 작다면 동일한 버킷에 담깁니다. 
* bucket_low (기본값 : 0.5)
  * SStable이 버켓에 포함되지 않으려면 버킷의 평균 크기보다 훨씬 작아야 하는가?
  * `bucket_low * avg_bucket_size < sstable_size`이라면 sstable은 버켓에 추가됩니다. 
* bucket_high  (기본값 : 1.5)
  * 버켓의 평균 크기보다 얼마나 크다면 SStable이 버켓이 포함되지 않는 지정하비다. 
  * `sstable_size < bucket_high * avg_bucket_size`이라면 sstable은 버켓에 추가됩니다.

#### https://www.datastax.com/blog/leveled-compaction-apache-cassandra
* 그림 1
  * size tiered compaction 으로 sstable 추가 
  * 시간이 지남에 따라서 여러 버전의 행이 다른 sstable에 존재할 수 있습니다.
  * 이러한 각 버전은 서로 다른 컬럼 집합이 있을 수 있습니다. 
  * 만약 sstable이 늘어나도록 허용 된 경우, 결과를 반환하기 위해서 행을 읽으려면 다른 파일에서 많은 검색이 필요할 수 있습니다.
  * 읽기 속도가 저하 되는 것을 방지하기 위해서 컴팩션이 백그라운드에서 실행되어 sstable을 병합합니다.
  * 행은 각 sstable에 기본키로 정렬되어 있기 때문에 ramdon i/o 없이 성능이 발휘됨
* 그림 2
  * 많은 insert 후에 size-tired compaction 아래의 sstable
  * 카산드라의 size-tired compaction 전략은 Google 빅테이블 문서에 설명된 것과 매우 유사합니다. 
  * 비슷한 크기의 sstable이 있을 때(기본적으로 4개 ) 그것들을 병합합니다. 
  * 그림 1에서 각 녹색 상자는 sstable을 나타내고 화살표는 compaction을 나타냅니다. 
  * 새로운 sstable이 생성되면 처음에는 아무일도 일어나지 않고 4개가 될때 마다 압축이 일어 남
  * 그림 2는 2단계의 sstable이 결합되어서 3단계가 되고 3단계가 결합되어서 4단계되는 것 같이 우리가 추후에 기대하는 것을 보여줍니다. 
  * 업데이트가 많은 워크로드에서 size-tiered compaction에는 세 가지 문제가 있습니다. 
    * 첫번째 
      * 얼마나 많은 sstable에 행이 분산되는지 보장되지 않기 때문에 성능이 일관되지 않습니다. 
      * 최악의 경우 각 sstable안에 조회하려는 행의 열이 있을 수 있습니다. 
    * 두번째 
      * 사용되지 않는 컬럼의 빠른 병합이 보장 되지 않아서 상당향 양의 공간이 낭비될 수 있습니다. 
      * 이는 삭제 비율이 높을 때 문제가 됩니다. 
    * 세번째
      * 반복되는 압축으로 인해 테이블이 커짐에 따라 공간도 문제가 될 수 있습니다.
      * 왜냐하면 병합된 sstable이 완전히 기록 될 때까지 쓸모없는 sstable 제거할 수 없기 때문입니다.  
      * 제거 할 쓸모없는 행이 없는 단일의 대형 테이블인 경우 
      * 카산드라는 병합된 테이블을 작성하기 위해 compaction 되는 테이블에서 사용하는 100%의 여유공간이 필요합니다. 
  * 그림3
    * 카산드라 1.0에서는 구글의 크로미움팀의 LevelDB를 기반으로 한 leveled compaction 전략을 도입했습니다. 
    * leveled compaction은 "levels"로 그룹화 된 고정된 작은 크기의 sstable을 생성합니다.(기본값 5MB)
    * 각 레벨 내에서 테이블은 겹치지 않도록 보장 됩니다. 
    * 각 레벨은 이전 레벨의 10배로 구성됩니다. 
    * 그림 3에서 새로운 sstable은 첫번째 수준인 L0에 추가되고 즉시 L1으로 압축됩니다. 
    * L1이 채워지면 추가 sstable은 L2로 이동합니다. 
    * L1에서 생성된 다음 sstable은 L2에 겹치는 sstable과 함께 압축됩니다. 
    * 더 많은 데이터가 추가됨에 따라서 수평 압축으로 인해 그림 4같은 상황이 발생
  * 그림 4
    * 많은 insert가 된후 leveled compaction에서 sstable
    * tiered compaction의 문제가 해결됩니다. 
      * leveled compaction은 모든 읽기의 90%가 안정된 sstable에서 충족되도록 보장합니다.(거의 균일한 행 크기 가정)
        * 최악의 경우는 총 레벨에 의해서 제한됨
          * 예를 들어 10TB는 7레벨
      * 사용되지 않는 행으로 인해 최대 10% 공간이 낭비됩니다.
      * 컴팩션을 위한 임시 공간이 테이블의 크기의 10배의 공간만 예약하면 됩니다.

#### 참조 사이트
* https://meetup.toast.com/posts/192
* https://cassandra.apache.org/doc/latest/operating/bloom_filters.html?highlight=bloom%20filter

## B-Tree
* 가장 널리 사용되는 색인 구조
* 범위 검색과 키-값 검색에 효율적 
* 페이지 단위로 데이터를 관리
* 페이지가 가득찰 경우 페이지 분할이 발생 -> 이것을 이용해서 트리는 balance를 이룬다\
* 분기계수(branching factor)
  * 하나의 페이지에서 참조하는 페이지의 숫자 
  * 분기계수가 500인 4kb는 한 깊이마다 500제곱으로 크기가 증가합니다. 

### B트리와 LSM 트리 비교
* 일반적으로 LSM 트리는 쓰기가 빠르고 읽기가 느림
  * 쓰기는 순차쓰기이고 읽기는 memtable, sstable을 순차적으로 읽어야 함

### LSM 트리 장점
* LSM은 순차쓰기 및 압축률 좋음
* 파편화 발생 안함
### LSM 트리 단점
* 컴팩션으로 인한 지연현상 발생
* 강력한 트랜젝션 지원 안됨

### Appendix
### 습득한 정보 응용
#### 카산드라는 일반적인 SQL과 다르게 Insert시 중복된 키에 대해서 업데이트가 발생하는가?
* LSM트리의 데이터 구조상 중복된 데이터는 컴팩션을 통해서 최신 데이터로 업데이트 됨
* 충돌 체크를 위한 비용이 비쌈!!
* 반면에 B-tree는 Insert시에 키에 해당하는 페이지 위치에 데이터를 기록하기 때문에 충돌 체크 바로 가능
#### 카산드라의 디자인 구조상 파티션 키를 10MB이하로 구성 해야 하는가?
* 카산드라의 각 노드들에게 분산되는 단위가 적당한 크기여야 효율적임
* LSM트리 구조상 데이터의 크기가 커질수록 데이터 검색의 비용이 커진다. 
  * 많은 memtable, sstable을 조회 해야함
* 일정 크기 이하로 분할 해 놓아야 최적의 성능을 보장할 수 있음
* 반면에 B-tree는 데이터가 커진다해도 검색의 비용이 그다지 커지지 않음.
  * 페이지의 깊이가 증가 할때마다 분기계수의 제곱만큼 데이터 크기가 커짐!
#### Paxos 프로토콜
* https://ko.wikipedia.org/wiki/%ED%8C%A9%EC%86%8C%EC%8A%A4_(%EC%BB%B4%ED%93%A8%ED%84%B0_%EA%B3%BC%ED%95%99)

#### 안티 캐싱
* 메모리가 충분하지 않을 때 최근에 사용하지 않는 데이터를 메모리에서 디스크로 내보내고 다시 접근시 적재하는 방법
* 가상메모리 기법과 비슷하지만 페이지 단위가 아닌 레코드 단위로 동작