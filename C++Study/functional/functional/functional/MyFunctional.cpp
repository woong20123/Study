#include <iostream>
#include "MyFunctional.h"

using namespace std;

int Func(double a, double b) {
	return (int)(a + b);
}



int main() {
	CFunction<int(double, double)> func1;
	func1 = &Func;
	cout << func1(1, 10) << endl;
	CFunction<int(double, double)> func2 = [](double a, double b) -> int { return a + b; };
	cout << func2(1, 10) << endl;
	return 0;
}