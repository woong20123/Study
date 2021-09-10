#include <iostream>
#include <thread>
#include <future>
#include <vector>
#include <numeric>
using namespace std::literals;

void add(std::promise<int> && p, int a, int b)
{
    std::this_thread::sleep_for(1s);
    p.set_value(a+b);
}

void consume(std::shared_future<int> sf){
    sf.get();
    std::cout << "finish foo" << std::endl;
}

int main()
{
    std::promise<int> pm;
    std::shared_future<int> ft = pm.get_future();

    std::thread t(add, std::move(pm), 10, 30);

    std::thread t1(consume, ft);
    std::thread t2(consume, ft);

    t.join();
    t1.join();
    t2.join();
} 