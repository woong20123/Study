import pizza.Calzone;
import pizza.NyPizza;
import pizza.Pizza;

public class main {
    public static void main(String[] args){
        NyPizza pizza = new NyPizza.Builder(NyPizza.Size.SMALL).addTopping(Pizza.Topping.SAUSAGE).addTopping(Pizza.Topping.ONION).build();
        System.out.print("NyPizza = ");
        for(var topping : pizza.getTopping()){
            System.out.printf("%s,", topping.toString());
        }
        System.out.println(" ");
        Calzone calzone = new Calzone.Builder().addTopping(Pizza.Topping.HAM).sauceInside().build();
        System.out.print("Calzone = ");
        for(var topping : calzone.getTopping()){
            System.out.print(topping.toString());
        }
        System.out.println(" ");
    }
}
