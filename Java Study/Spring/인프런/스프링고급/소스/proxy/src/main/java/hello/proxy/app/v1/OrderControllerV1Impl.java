package hello.proxy.app.v1;

import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;

@RequiredArgsConstructor
public class OrderControllerV1Impl implements OrderControllerV1{

    private final OrderServiceV1 orderService;

    @Override
    public String request(String itemId){
        orderService.orderItem(itemId);
        return "ok";
    }

    @Override
    public String noLog(){
        return "ok";
    }
}
