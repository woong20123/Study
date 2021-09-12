#include <iostream>
#include <thread>
#include <future>

// add 함수가 promise를 통해서 반환 값 전달
// 다른 스레드에 전달
void add(std::promise<int> && p, int a, int b)
{
    p.set_value(a+b);
}

int main()
{
    std::promise<int> pm;
    std::future<int> ft = pm.get_future();

    std::thread t(add, std::move(pm), 10, 30);
    ft.get();

    t.join();
}