@FunctionalInterface
interface MyFunction {
    void myMethod();     // public abstarct void run();
}

class Outer {
    int val = 10; // Outer.this.val;s

    class Inner {
        int val = 20; // this.val;

        void method(final int i){
            int val = 30; // final int val = 30;
            //i = 10;

            MyFunction f = () -> {
                System.out.println("                  i : " + i);
                System.out.println("                val : " + val);
                System.out.println("           this.val : " + this.val);
                System.out.println("     Outer.this.val : " + Outer.this.val);
            };

            f.myMethod();
        }
    }
}

class LambdaEx02{
   
    public static void main(String[] args){
        Outer outer= new Outer();
        Outer.Inner inner = outer.new Inner();
        inner.method(100);
    }
}