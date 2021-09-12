#include <iostream>
#include <thread>
#include <chrono>
#include <future>
using namespace std::literals;

int add(int a, int b){
    std::cout << "start add : thread id " << std::this_thread::get_id() << std::endl;
    std::this_thread::sleep_for(2s);
    std::cout << "finish add : thread id " << std::this_thread::get_id() << std::endl;
    return a + b;
}

int main()
{
    // 임시 객체가 소멸되면서 get()을 호출
    std::async(std::launch::async, add, 10, 20);

    std::cout << "continue main" << std::endl;

    //int ret = ft.get();
}