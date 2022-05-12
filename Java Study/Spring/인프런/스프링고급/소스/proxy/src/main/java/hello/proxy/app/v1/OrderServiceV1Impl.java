package hello.proxy.app.v1;

import lombok.RequiredArgsConstructor;
import org.springframework.core.annotation.Order;

@RequiredArgsConstructor
public class OrderServiceV1Impl implements OrderServiceV1 {
    private final OrderRepositoryV1 orderRepository;

    @Override
    public void orderItem(String itemId){
        orderRepository.save(itemId);
    }

}
