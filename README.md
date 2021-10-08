# tracking-portfolio
Mixed Integer Quadratic Programming extension for "Mr Greedy and the Tale of the Minimum Tracking Error Variance"

The unrounded target position usually was 𝑝_𝑖=𝑓_𝑖 𝑐𝑝𝑢_𝑖, where 𝑝_𝑖=target position, 𝑓_𝑖=instrument forecast, 𝑐𝑝𝑢_𝑖=contracts per unit for each instrument i. Note all these quantities are real (not rounded values).


Modelling the tracking covariance is straightforward:

![image](https://user-images.githubusercontent.com/5354945/136517788-be2ea4d3-f2ff-4f96-af65-a482eed69f92.png)


where x is defined as an integer variable representing the number of contracts of the portfolio, 𝜌_𝑚𝑘𝑡  is the correlation matrix and 𝜇 encapsulates the target risk multiplier with regards to the unit size.


For the execution costs I need to model to model |𝑥−𝑥_0 | with and extra variable z and additional constraints:
![image](https://user-images.githubusercontent.com/5354945/136517917-e558f395-6695-4e83-b113-07105c400cc3.png)

and define the total cost as
![image](https://user-images.githubusercontent.com/5354945/136518170-c2b1a669-83ac-44cb-bb57-63627245f66e.png)

We minimize 

![image](https://user-images.githubusercontent.com/5354945/136518288-d7b07fc7-0df2-4345-a4ab-26bccf87db5a.png)

𝜆 being the shadow cost.

Note that minimizing C will imply 𝑧_𝑖= |𝑥−𝑥_0 |.
Note that we are not minimizing the tracking standard deviation, but the tracking covariance. Thus, the shadow cost has a different meaning to the one Rob is using.
