package pizza;

import java.util.EnumSet;
import java.util.Set;

public abstract class Pizza{
    public enum Topping { HAM, MUSHROOM, ONION, PEPPER, SAUSAGE}
    final Set<Topping> toppings;

    public Set<Topping> getTopping(){
        return toppings;
    }

    public abstract static class Builder<T extends Builder<T>>{
        EnumSet<Topping> toppings = EnumSet.noneOf(Topping.class);
        public T addTopping(Topping topping){
            toppings.add(topping);
            return self();
        }

        abstract Pizza build();

        // 하위 클래스는 반드시 이 메서드를 재정의 해서 this를 반환해야 함
        protected abstract T self();
    }

    Pizza(Builder<?> builder){
        toppings = builder.toppings.clone();
    }
}