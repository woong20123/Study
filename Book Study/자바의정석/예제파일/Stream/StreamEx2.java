import java.util.*;
import java.util.stream.*;


class StreamEx2{
    public static void main(String[] args){
        Student[] stuArr  = {
            new Student("이자바", 3, 300),
            new Student("김자바", 1, 200),
            new Student("안자바", 2, 100),
            new Student("박자바", 2, 150),
            new Student("소자바", 1, 200),
            new Student("나자바", 3, 290),
            new Student("감자바", 3, 180)
        };

        Stream<Student> studentStream = Stream.of(stuArr);

        studentStream.sorted(Comparator.comparing(Student::getBan)      // 반별 정렬
        .thenComparing(Comparator.naturalOrder()))                      // 기본 정렬
        .forEach(System.out::println);

        studentStream = Stream.of(stuArr);
        IntStream stuScoreStream = studentStream.mapToInt(Student::getTotalScore);

        var stat = stuScoreStream.summaryStatistics();
        System.out.println("count=" + stat.getCount());
        System.out.println("sum=" + stat.getSum());
        System.out.println("average=" + stat.getAverage());
        System.out.println("min=" + stat.getMin());
        System.out.println("max=" + stat.getMax());

        String[] lineArr = {
            "Belive or not It is true",
            "Do or do not There is no try",
        };

        Stream<String> lineStream = Arrays.stream(lineArr);
        Stream<String> strStrema = lineStream.flatMap(line->Stream.of(line.split(" +")));
        strStrema.forEach(System.out::print);

    }
}

class Student implements Comparable<Student>{
    String name;
    int ban;
    int totalScore;
    Student(String name, int ban, int totalScore){
        this.name = name;
        this.ban = ban;
        this.totalScore = totalScore;
    }

    public String toString() {
        return String.format("[%s, %d, %d]", name, ban, totalScore);
    }

    String getName() { return name; }
    int getBan() { return ban; }
    int getTotalScore() { return totalScore; }

    public int compareTo(Student s){
        return s.totalScore - this.totalScore;
    }
}