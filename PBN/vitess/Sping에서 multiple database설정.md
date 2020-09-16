## 목적
#### vitess의 경우 keyspace를 통한 분산도 지원한다. 
#### keyspace로 분할된 경우 서로 다른 DB로 설정되기 때문에 DB접속을 다중 셋팅이 필요하다.

## 설정 파일 셋팅
#### application.properties
```ini
spring.datasource.driver-class-name=com.mysql.cj.jdbc.Driver

spring.datasource.keyspace1.jdbc-url=jdbc:mysql://vitess-alpha.ncsoft.com:31199/pbn_test
spring.datasource.keyspace1.username=pbn
spring.datasource.keyspace1=pbn!2345

spring.datasource.keyspace2.jdbc-url=jdbc:mysql://vitess-alpha.ncsoft.com:31199/pbn_test2
spring.datasource.keyspace2.username=pbn
spring.datasource.keyspace2=pbn!2345
```
## 테스트할 엔티티 예제
```java
@Entity
public class product{
	
	public product() {}
	public product(byte[] sku, byte[] description, long price) {
		
		this.sku = sku;
		this.description = description;
		this.price = price;
	}

	@Id
	private byte[] sku;
	
	@Column(nullable = true)
	private byte[] description;
	
	@Column(nullable = true)
	private long price;
```
## Repository 생성
#### Repository의 위치를 구분해서 DB 접근을 구분할 수 있습니다. 
#### 예를 들어 vitess와 mysql의 연결을 구분지으려면 
* 엔티티 정의 위치 : package com.example.PBNLastManager.Vitess.Domain
* Repository 정의 위치
  * package com.example.PBNLastManager.Vitess.Keyspace1
```java
public interface productRepository extends CrudRepository<product, byte[]>
{
	product findBySku(byte[] sku);
	List<product> findByPrice(long price);
}
```
  * package com.example.PBNLastManager.Vitess.Keyspace2
```java
public interface productRepository extends CrudRepository<product, byte[]>
{
	product findBySku(byte[] sku);
	List<product> findByPrice(long price);
}
```
## DataSource 설정
### keyspace1 설정
### keyspace2 설정