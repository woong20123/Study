package hello.proxy.pureproxy.concreteproxy.code;

public class ConcreteClient {

    private ConcreteLogic concreateLogic;

    public ConcreteClient(ConcreteLogic concreateLogic) {
        this.concreateLogic = concreateLogic;
    }

    public void execute(){
        concreateLogic.operation();
    }
}
