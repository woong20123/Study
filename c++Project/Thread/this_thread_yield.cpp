#include <iostream>
#include <thread>
#include <chrono>
using namespace std::literals;

void mysleep(std::chrono::microseconds us){
    auto target = std::chrono::high_resolution_clock::now() + us;

    int a = 0;
    while(std::chrono::high_resolution_clock::now() < target){
        std::this_thread::yield();
        ++a;
    }
        
    std::cout << "yield count = " << a << std::endl;
}

int main() {
    mysleep(1ms);
}