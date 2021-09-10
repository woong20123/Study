#include <iostream>
#include <thread>
#include <future>
#include <vector>
#include <numeric>

int main(){
    std::vector<int> v1 = {1,2,3,4,5,6,7,8,9,10};
    std::vector<int> v2(10);

    std::promise<void> pm1;
    std::future<void> ft1 = pm1.get_future();

    std::promise<int> pm2;
    std::future<int> f2 = pm2.get_future();

    std::thread t( [&]{
        std::partial_sum(v1.begin(), v1.end(), v2.begin());\
        pm1.set_value();    // 첫번째 연산이 끝났음을 알림
        int s = std::accumulate(v2.begin(), v2.end(), 0);
        pm2.set_value(s);
    });

    ft1.get();
    for( auto n : v2)
        std::cout<< n << "," ;

    int s = f2.get();
    std::cout << "\n" << s << std::endl;

    t.join();
}