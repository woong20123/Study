#include <iostream>
#include <thread>
#include <chrono>
#include <future>
using namespace std::literals;

int add(int a, int b){
    std::this_thread::sleep_for(2s);
    std::cout << "add thread id " << std::this_thread::get_id() << std::endl;
    return a + b;
}

int main()
{
    //add(10, 20);
    std::future<int> ft = std::async(add, 10, 20);

    int ret = ft.get();

    std::cout << "main thread id " << std::this_thread::get_id() << std::endl;

    std::cout << "result : " << ret << std::endl;
}