javac %1.java -encoding utf-8
java -classpath ".;lib" %1
del *.class