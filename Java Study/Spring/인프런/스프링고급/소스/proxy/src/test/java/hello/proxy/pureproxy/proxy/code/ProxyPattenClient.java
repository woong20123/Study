package hello.proxy.pureproxy.proxy.code;

public class ProxyPattenClient {
    private Subject subject;

    public ProxyPattenClient(Subject subject){
        this.subject = subject;
    }

    public void execute() {
        subject.operation();
    }
}
