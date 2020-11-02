## 필요한 의존성
* JDBC API
* MYSQL Driver
* Spring Data JPA

## DB 연결을 위한 환경변수 
src/resources/application.properties에 데이터 추가 필요 
* 예제 코드
```java
spring.datasource.driver-class-name=com.mysql.cj.jdbc.Driver
spring.datasource.url=jdbc:mysql://localhost:3306/demo
spring.datasource.username=demouser
spring.datasource.password=Demouser!23
spring.datasource.initialize=true
spring.jpa.hibernate.ddl-auto=update
spring.jpa.show-sql=true
```

### Tip application.properties이란?
  * [링크](./SpringBoot%20설정하기.md)

### Entity 클래스 작성 예제
```java
package com.example.PBNFlowManager;

import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;

@Entity
// Customer라는 엔티티 클래스를 작성합니다. 
public class Customer{
	@Id
	@GeneratedValue(strategy=GenerationType.AUTO)
	private Long id;
	private String firstName;
	private String lastName;
	
	protected Customer() {}
	
	public Customer(String firstName, String lastName) {
		this.firstName = firstName;
		this.lastName = lastName;
	}
	
	@Override
	public String toString() {
		return String.format(
				"Customer[id=%d, firstName='%s', lastName='%s']",
				id, firstName, lastName);
	}
	
	public Long getId() {
		return id;
	}
	
	public String getFirstName() {
		return firstName;
	}
	
	public String getLastName() {
		return lastName;
	}
}
```
### 
```java
import org.springframework.data.repository.CrudRepository;

// CrudRepository 상속받아서 구현합니다.
// CrudRepository는 해당 엔티티를 위한 CRUD를 지원합니다.
// PagingAndSortingRepository는 CrudRepository를 확장해서 지원합니다. 
// 페이징 처리가 가능합니다. 
public interface CustomerRepository extends CrudRepository<Customer, Long>
{
	List<Customer> findByLastName(String lastName);
	Customer findById(long id);
}
```

## Spring에서 bulk insert 설정하기
### spring.datasource.result.jdbc-url에 "&rewriteBatchedStatements=true" 옵션 추가 
```ini
spring.datasource.result.jdbc-url=jdbc:mysql://172.19.153.153:3306/pbn?serverTimezone=Asia/Seoul&rewriteBatchedStatements=true
```
### jdbcTemplate를 사용해서 batchUpdate 사용
* 구현 예제
```java
@Override
	public void saveAll(List<PersonalStrava> items) {
		// TODO Auto-generated method stub
		int batchCount = 0;
		List<PersonalStrava> subItems = new ArrayList<>();
		for(int i = 0; i < items.size(); i++) {
			subItems.add(items.get(i));
			if((i + 1)  % batchSize == 0) {
				batchCount = batchInsert(batchSize, batchCount, subItems);
			}
		}
		
		if (!subItems.isEmpty()) {
			batchCount = batchInsert(batchSize, batchCount, subItems);
		}
		System.out.println("batchCount: " + batchCount + " products size : " + items.size());
	}
	
	private int batchInsert(int batchSize, int batchCount, List<PersonalStrava> subItems) {
		jdbcTemplate.batchUpdate("INSERT INTO `pbn`.`personal_strava` (`log_date`,`instance_id`,`partition_seq`,`time`,`row_seq`,`actor_id`,`total_exp`)"
				+ "VAlUES( ?, ?, ?, ?, ?, ?, ?)",
				new BatchPreparedStatementSetter() {
					@Override
					public void setValues(PreparedStatement ps, int i) throws SQLException {
						// TODO Auto-generated method stub
						ps.setString(1, subItems.get(i).getLogDate());
						ps.setString(2, subItems.get(i).getInstanceId());
						ps.setString(3, subItems.get(i).getPartitionSeq());
						ps.setString(4, subItems.get(i).getTime());
						ps.setString(5, subItems.get(i).getRowSeq());
						ps.setString(6, subItems.get(i).getActorId());
						ps.setLong(7, subItems.get(i).getTotalExp());
					}
					
					@Override
					public int getBatchSize() {
						return subItems.size();
					}
				});
		subItems.clear();
		batchCount++;
		return batchCount;
	}
```

### 참조 사이트 
* https://spring.io/guides/gs/accessing-data-jpa/#scratch

### spring rest doc
* https://docs.spring.io/spring-restdocs/docs/2.0.5.RELEASE/reference/html5/
  

