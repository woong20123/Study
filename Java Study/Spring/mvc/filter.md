### OncePerRequestFilter와 Filter 차이
* Filter는 javax.servlet-api와 tomcat-embed-core를 사용하면 제공되는 인터페이스 
```java
    // 필터 동작 방식
    // Before라는 문자열이 출력되고 
    // 서블릿이 실행되고 After 문자열 출력 
    @Override
    public void doFilter(
      ServletRequest request, 
      ServletResponse response, 
      FilterChain chain) throws IOException, ServletException{
            System.out.println("Before");
            chain.doFilter(request, response);
            System.out.println("After");
    }
```
* OncePerRequestFilter은 모든 서블릿에 일관된 요청을 처리하기 위해 만들어진 필더 
  * 사용자의 한번에 요청 당 한번만 호출됨을 보장합니다. 