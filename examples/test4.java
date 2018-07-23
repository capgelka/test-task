public class Main {

    public static void method(boolean... conditions) {

        int x;

        x = 1;

        if (conditions[0]) {

            x = 2;

            if (conditions[1]) {

                x = 3;

            }

        }

        if (conditions[1]) {

            if (conditions[0]) {
                x = 5;
            }

        }

        System.out.println(x);

    }

}