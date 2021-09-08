#include <iostream>
#include <thread>

void foo(int a, double d) {}

struct Machine{
    void Run(int a, double d){}
};

struct Work
{
    void operator()(int a, double d) const { }
};

int main(){
    Machine m;
    Work w;

    std::thread t1(&foo, 1, 3,4);
    //std::thread t2(&Machine::Run, &m, 1, 3,4);
    //std::thread t3(w, 1, 3,4);
    //std::thread t4([]{std::cout << "lambda" << std::endl; });

    t1.join();
    t2.join();
    t3.join();
    t1.join();
}