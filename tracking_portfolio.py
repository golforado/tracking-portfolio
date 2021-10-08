import cvxpy as cp
import numpy as np

def tracking_portfolio( correlation_matrix, instrument_forecasts, contracts_per_unit, cost_per_trade, initial_contracts, unit_size, shadow_cost):
    N = len(instrument_forecasts)
    opt_contracts = cp.Variable(N, integer=True)
    trades = cp.Variable( N )
    
    #absolute value modelling
    constraints_0 = [ trades >= opt_contracts - initial_contracts ]
    constraints_1 = [ trades >= -(opt_contracts - initial_contracts) ]
    
    constraints = constraints_0 + constraints_1
    tracking_weights = instrument_forecasts-opt_contracts/contracts_per_unit
    
    tracking_covar = unit_size*unit_size*cp.quad_form(tracking_weights, correlation_matrix)
    exec_cost = trades @ cost_per_trade
    
    objective = cp.Minimize(tracking_covar + shadow_cost * exec_cost)
    prob = cp.Problem(objective, constraints)
    prob.solve(solver='CPLEX')#, verbose=True)

    print("Status: ", prob.status)
    print("The optimal value is", prob.value)
    #print([x for x in opt_contracts.value])
    print("tracking_std", np.sqrt(tracking_covar.value))
    print("costs",exec_cost.value)
    return opt_contracts.value
    
    
    
if __name__ == "__main__":
    f = open("trackingPortfolioArgs.json", "r")
    data = eval(f.read())
    f.close()
    initial_contracts = data['InitialContracts']
    instrument_forecasts = np.array(data['InstrumentForecasts'])
    contracts_per_unit = data['ContratsPerUnit']
    cost_per_trade = data['CostPerTrade']
    correlation_matrix = np.matrix(data['CorrelationMatrix'])
    shadow_cost = data['ShadowCost']
    unit_size = data['UnitSize']
    max_contracts = data['MaxContracts']
    labels = data['labels']

    opt_contracts = tracking_portfolio(correlation_matrix, instrument_forecasts, contracts_per_unit, cost_per_trade, initial_contracts, unit_size, shadow_cost)
    for l,c in zip(labels, opt_contracts):
        print(l,c)