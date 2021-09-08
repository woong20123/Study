#include <iostream>
#include <functional>

void foo(int & a) { a = 200;}

template<typename T> void call_foo(T arg){
    foo(arg);
}

int main()
{
    int n = 0; 

    call_foo(std::ref(n));
    //call_foo(n);
    //foo(n);

    std::cout << n << std::endl;
}