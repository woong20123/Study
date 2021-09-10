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
    // set_value를 두번 사용할 수 없음
    //p.set_value(a+b); 
}

int main()
{
    std::promise<int> pm;
    std::future<int> ft = pm.get_future();

    std::thread t(add, std::move(pm), 10, 30);

    std::cout << ft.valid() << std::endl;
    ft.get();
    std::cout << ft.valid() << std::endl;

    t.join();
} 