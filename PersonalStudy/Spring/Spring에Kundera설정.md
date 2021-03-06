## Kundera Document 사이트 
### https://github.com/Impetus/Kundera/wiki/Getting-Started-in-5-minutes

## 설정파일 
### dependency 추가하기
```xml
<dependency>
	<groupId>com.impetus.kundera.core</groupId>
	<artifactId>kundera-core</artifactId>
	<version>3.13</version>
</dependency>
<dependency>
	<groupId>com.impetus.kundera.client</groupId>
	<artifactId>kundera-cassandra</artifactId>
	<version>3.13</version>
</dependency>
```
### persistence.xml 파일 추가 
```xml
<persistence xmlns="http://java.sun.com/xml/ns/persistence" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
             xsi:schemaLocation="http://java.sun.com/xml/ns/persistence
	http://java.sun.com/xml/ns/persistence/persistence_2_0.xsd"
             version="2.0">
    <persistence-unit name="cassandra_pu">
        <provider>com.impetus.kundera.KunderaPersistence</provider>
        <class>org.davidcampos.cassandra.kundera.UserKundera</class>
        <exclude-unlisted-classes>true</exclude-unlisted-classes>
        <properties>
            <property name="kundera.nodes" value="cassandra"/>
            <property name="kundera.port" value="9160"/>
            <property name="kundera.keyspace" value="example"/>
            <property name="kundera.dialect" value="cassandra"/>
            <property name="kundera.ddl.auto.prepare" value="update"/>
            <property name="kundera.client.lookup.class"
                      value="com.impetus.client.cassandra.thrift.ThriftClientFactory"/>
        </properties>
    </persistence-unit>
</persistence>
```