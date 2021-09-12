#include <iostream>
#include <thread>
#include <future>

// 멀티 스레드 고려하지 않고 작성된 함수 
int add(int a, int b)
{
    std::cout << "add" << std::endl;
    return a + b;
}

int main()
{
    std::packaged_task<int(int, int)> task(add);
    std::future<int> ft =  task.get_future();
    
    std::thread t(std::move(task), 10, 20);
    std::cout << "continue main" << std::endl;

    int ret = ft.get();
    std::cout << ret << std::endl;
    t.join();
}