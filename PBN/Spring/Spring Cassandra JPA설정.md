## 필요한 의존성
* Spring Data for Apache Cassandra
* Srping Data JPA

## DB 연결을 위한 환경변수 
```ini
spring.data.cassandra.keyspace-name=pbn_test_keyspace
spring.data.cassandra.contact-points=172.19.153.86:9042
spring.data.cassandra.local-datacenter=datacenter1
spring.data.cassandra.request.timeout = 20s
spring.data.cassandra.request.page-size=100000
```

## 복합 키를 사용하는 Entity 클래스 작성 예제
#### 카산드라는 Primary key를 표현하기 위한 키 클래스가 필요함
#### plogid와 plogdata를 partition key입니다. 
#### date를 seq는 clustering key입니다.
```java
@PrimaryKeyClass
public @Data class pbnBigTableKey implements Serializable {
	// type은 키가 파티션
	@PrimaryKeyColumn(type = PrimaryKeyType.PARTITIONED, ordinal = 0) 
	private String plogid;
	
	@PrimaryKeyColumn(type = PrimaryKeyType.PARTITIONED, ordinal = 1) 
	private String plogdate;

	@PrimaryKeyColumn(type = PrimaryKeyType.CLUSTERED, ordinal = 2)
	private String time;
	
	@PrimaryKeyColumn(type = PrimaryKeyType.CLUSTERED, ordinal = 3)
	private long seq;
	
	public pbnBigTableKey(final String plogid, final String plogdate, final String time, final long seq) {
	    this.plogid = plogid;
	    this.plogdate = plogdate;
	    this.time = time;
	    this.seq = seq;
	  }
}
```
#### Entity 클래스
```java
@Table(value = "pbn_big_table")
@AllArgsConstructor
@NoArgsConstructor
public @Data class  pbnBigTable {
	
	@PrimaryKey
	pbnBigTableKey key;
	
	private String actor_account_id;
	private long actor_born;
	private long actor_class;
	private long actor_code;
	private long actor_guild;
	private long actor_id;
	// ... 생략
}
```
#### Repository 클래스 예제
```java
import com.example.PBNLastManager.cassandra.Domain.pbnBigTable;
import com.example.PBNLastManager.cassandra.Domain.pbnBigTableKey;

public interface pbnBigTableRepository extends CassandraRepository<pbnBigTable, pbnBigTableKey>{
	Slice<pbnBigTable> findByKeyPlogidAndKeyPlogdate(final String plogid, final String plogdate, Pageable pageable );
	List<pbnBigTable> findByKeyPlogidAndKeyPlogdateAndKeyTime(final String plogid, final String plogdate, final String time);
}
```
#### 사용 예제
#### 카산드라의 경우 대량의 데이터를 가져올 경우 paging로 처리하는 예제
```java
    @Autowired
	private pbnBigTableRepository pbnBigTableRepository;

    // 테스트 로직 
    @Bean
	public CommandLineRunner demo(userinfoRepository userRepository, productRepository productRespository, customerRepository customerRepository
			,corderRepository corderRepository, resultlogexpRepository resultlogexpRepository, pbnBigTableRepository pbnBigTableRepository) {
		return (args)->{
            log.info("Cassandra BigTable found with findAll():");
			log.info("-------------------------------");
		
			//var list = pbnBigTableRepository.findByKeyPlogidAndKeyPlogdateAndKeyTime("1012", "20200909", "2020-09-09 13:10:05");
			
            // 페이지를 가져 올 때 몇개씩 가져올지 page 갯수를 지정합니다.
			Pageable pageable = PageRequest.of(0, 50000);
			while(true) {
				//var slice = pbnBigTableRepository.findAll(pageable);
				var slice = pbnBigTableRepository.findByKeyPlogidAndKeyPlogdate("1012", "20200909",pageable);
				log.info("slice.getNumber() = {}", slice.getNumber());
				log.info("slice.getNumberOfElements() = {}", slice.getNumberOfElements());
				log.info("slice.getSize() = {}", slice.getSize());
				log.info("{}",slice.getContent().get(0).toString());
				if(!slice.hasNext())
				{break;}
				pageable = slice.nextPageable();
			}
        };
    }
```
