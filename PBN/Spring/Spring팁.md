## java에서 lombok을 사용하기 
#### lombok의 jar 파일이 생성된 폴더로 이동합니다. 
* 이클립스에서 Maven Dependency를 참조하면 됩니다. 
#### 해당위치에서 해당 버전의 lombok을 설치합니다. 
```java
 java -jar .\lombok-1.18.12.jar
```
#### 이클립스 실행 폴더의 eclipse.ini에 속성 추가
```ini
-xbootclasspath/a:lombok.jar
-javaagent:lombok.jar
```

### 참조 
* https://siyoon210.tistory.com/24
* https://ojava.tistory.com/131