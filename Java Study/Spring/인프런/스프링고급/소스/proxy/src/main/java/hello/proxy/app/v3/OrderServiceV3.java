package hello.proxy.app.v3;

import hello.proxy.app.v3.OrderRepositoryV3;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class OrderServiceV3 {

    private final OrderRepositoryV3 orderRepository;

    public void orderItem(String itemId){
        orderRepository.save(itemId);
    }

}
