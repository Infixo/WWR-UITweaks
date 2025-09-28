using HarmonyLib;
using STM.Data.Entities;
using STM.GameWorld;
using STM.GameWorld.AI;
using STM.GameWorld.Commands;
using STM.GameWorld.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UITweaks.Patches;


public static class PlanEvaluateRoute_Patches
{


    private void EvaluateLine(Company company, VehicleBaseUser[] vehicles, GameScene scene, HubManager manager = null)
    {
        VehicleEvaluation _evaluation = default(VehicleEvaluation);
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i].Age > 0)
            {
                _evaluation.Evaluate(vehicles[i]);
            }
        }
        if (!_evaluation.Downgrade)
        {
            VehicleBaseUser _user = GetWorst(vehicles);
            if (_user == null || _user.Balance.GetCurrentMonth() >= -10000000)
            {
                if (_evaluation.Upgrade)
                {
                    VehicleBaseUser _best = GetBest(vehicles);
                    decimal _best_e = (decimal)(_best.Efficiency.GetBestOfTwo() * _best.Passengers.Capacity) / 100m + (decimal)_best.Route.GetWaiting() / 24m / (decimal)vehicles.Length;
                    NewRouteSettings _settings = new NewRouteSettings(_best);
                    int _range2 = GetRange(vehicles);
                    if (_best.evaluated_on != _best.stops && new VehicleEvaluation(_best).Upgrade)
                    {
                        VehicleBaseEntity _upgrade = GetUpgrade(company, _best, manager, _range2);
                        if (_upgrade != null)
                        {
                            _settings.SetVehicleEntity(_upgrade);
                            _best.evaluated_on = _best.stops;
                            if (_best_e > (decimal)_upgrade.Real_min_passengers)
                            {
                                if (manager != null)
                                {
                                    long _price6 = _upgrade.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                                    _price6 -= _best.GetValue();
                                    if (_price6 == 0L)
                                    {
                                        _price6 = 1L;
                                    }
                                    decimal _weight4 = GetBalance(_best) * 2;
                                    manager.AddNewPlan(new GeneratedPlan(_weight4 / (decimal)_price6, _settings, _price6, _best));
                                }
                                else if (company.AI != null)
                                {
                                    long _price5 = _upgrade.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                                    _price5 -= _best.GetValue();
                                    decimal _weight3 = GetBalance(_best) * 2;
                                    if (_price5 == 0L)
                                    {
                                        _price5 = 1L;
                                    }
                                    company.AI.AddNewPlan(new GeneratedPlan(_weight3 / (decimal)_price5, _settings, _price5, _best));
                                }
                                else
                                {
                                    scene.Session.AddEndOfFrameAction(delegate
                                    {
                                        _settings.upgrade = new UpgradeSettings(_best, scene);
                                        scene.Session.Commands.Add(new CommandSell(company.ID, _best.ID, _best.Type));
                                        scene.Session.Commands.Add(new CommandNewRoute(company.ID, _settings, open: false));
                                    });
                                }
                                return;
                            }
                            _best.evaluated_on = _best.stops;
                            if (_evaluation.samples > 1)
                            {
                                VehicleBaseUser _worst2 = GetNextDowngrade(vehicles);
                                _best_e += (decimal)(_worst2.Efficiency.GetBestOfTwo() * _worst2.Passengers.Capacity) / 90m;
                                if (_best_e > (decimal)_upgrade.Real_min_passengers)
                                {
                                    scene.Session.Commands.Add(new CommandSell(company.ID, _worst2.ID, _worst2.Type, manager));
                                    if (manager != null)
                                    {
                                        long _price4 = _upgrade.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                                        _price4 -= _best.GetValue();
                                        decimal _weight2 = GetBalance(_best) * 2;
                                        if (_price4 == 0L)
                                        {
                                            _price4 = 1L;
                                        }
                                        manager.AddNewPlan(new GeneratedPlan(_weight2 / (decimal)_price4, _settings, _price4, _best));
                                    }
                                    else if (company.AI != null)
                                    {
                                        long _price3 = _upgrade.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                                        _price3 -= _best.GetValue();
                                        decimal _weight = GetBalance(_best) * 2;
                                        if (_price3 == 0L)
                                        {
                                            _price3 = 1L;
                                        }
                                        company.AI.AddNewPlan(new GeneratedPlan(_weight / (decimal)_price3, _settings, _price3, _best));
                                    }
                                    else
                                    {
                                        scene.Session.AddEndOfFrameAction(delegate
                                        {
                                            _settings.upgrade = new UpgradeSettings(_best, scene);
                                            scene.Session.Commands.Add(new CommandSell(company.ID, _best.ID, _best.Type));
                                            scene.Session.Commands.Add(new CommandNewRoute(company.ID, _settings, open: false));
                                        });
                                    }
                                    return;
                                }
                            }
                        }
                    }
                    if (!_best.Entity_base.CanBuy(company, _best.Hub.Longitude) || _evaluation.samples >= 3)
                    {
                        return;
                    }
                    _best_e /= 2m;
                    if (_best_e <= (decimal)_best.Entity_base.Real_min_passengers)
                    {
                        return;
                    }
                    if (manager != null)
                    {
                        long _price2 = _best.Entity_base.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                        if (_best.Hub.Full())
                        {
                            _price2 += _best.Hub.GetNextLevelPrice(scene.Session);
                        }
                        manager.AddNewPlan(new GeneratedPlan(1m, new NewRouteSettings(_best), _price2));
                        return;
                    }
                    if (company.AI != null)
                    {
                        long _price = _best.Entity_base.GetPrice(scene, company, scene.Cities[_best.Hub.City].User);
                        if (_best.Hub.Full())
                        {
                            _price += _best.Hub.GetNextLevelPrice(scene.Session);
                        }
                        company.AI.AddNewPlan(new GeneratedPlan(1m, new NewRouteSettings(_best), _price));
                        return;
                    }
                    if (_best.Hub.Full())
                    {
                        if (company.Wealth < _best.Hub.GetNextLevelPrice(scene.Session))
                        {
                            return;
                        }
                        scene.Session.Commands.Add(new CommandUpgradeHub(company.ID, _best.Hub.City));
                    }
                    scene.Session.Commands.Add(new CommandNewRoute(company.ID, new NewRouteSettings(_best), open: false));
                }
                else if (vehicles.Length > 1)
                {
                    VehicleBaseUser _worst = GetWorst(vehicles);
                    if (_worst != null && _worst.evaluated_on != _worst.stops && _worst.Age > 3 && _worst.Balance.GetRollingYear() < 0)
                    {
                        scene.Session.Commands.Add(new CommandSell(company.ID, _worst.ID, _worst.Type, manager));
                    }
                }
                else if (AllVehiclesNotDelivering(vehicles))
                {
                    CheckIndirect(company, vehicles, scene, manager);
                }
                return;
            }
        }
        int _range = GetRange(vehicles);
        while (_evaluation.Downgrade && _evaluation.samples > 1)
        {
            _evaluation.samples--;
            VehicleBaseUser _worst4 = GetNextDowngrade(vehicles);
            if (_worst4 == null || _worst4.evaluated_on == _worst4.stops || (_worst4.Balance.GetBestOfTwo() > 0 && _worst4.Balance.GetLastMonth() + _worst4.Balance.GetCurrentMonth() > -10000000))
            {
                return;
            }
            VehicleBaseEntity _downgrade2 = GetDowngrade(company, _worst4, _range);
            if (_downgrade2 != null)
            {
                scene.Session.AddEndOfFrameAction(delegate
                {
                    NewRouteSettings newRouteSettings2 = new NewRouteSettings(_worst4);
                    newRouteSettings2.SetVehicleEntity(_downgrade2);
                    newRouteSettings2.upgrade = new UpgradeSettings(_worst4, scene);
                    scene.Session.Commands.Add(new CommandSell(company.ID, _worst4.ID, _worst4.Type, manager));
                    scene.Session.Commands.Add(new CommandNewRoute(company.ID, newRouteSettings2, open: false, manager));
                });
            }
            else
            {
                scene.Session.Commands.Add(new CommandSell(company.ID, _worst4.ID, _worst4.Type, manager));
            }
            VehicleEvaluation _e = new VehicleEvaluation(_worst4);
            _evaluation.Remove(_e);
        }
        if (!_evaluation.Downgrade || _evaluation.samples != 1)
        {
            return;
        }
        VehicleBaseUser _worst3 = GetNextDowngrade(vehicles);
        if (_worst3.evaluated_on == _worst3.stops)
        {
            return;
        }
        VehicleBaseEntity _downgrade = GetDowngrade(company, _worst3, _range);
        if (_downgrade != null)
        {
            scene.Session.AddEndOfFrameAction(delegate
            {
                NewRouteSettings newRouteSettings = new NewRouteSettings(_worst3);
                newRouteSettings.SetVehicleEntity(_downgrade);
                newRouteSettings.upgrade = new UpgradeSettings(_worst3, scene);
                scene.Session.Commands.Add(new CommandSell(company.ID, _worst3.ID, _worst3.Type, manager));
                scene.Session.Commands.Add(new CommandNewRoute(company.ID, newRouteSettings, open: false, manager));
            });
        }
        else if (_worst3.Entity_base.Tier == 1 && _worst3.Age >= 6 && _worst3.Balance.GetRollingYear() < 10000000 && manager == null)
        {
            scene.Session.Commands.Add(new CommandSell(company.ID, _worst3.ID, _worst3.Type, manager));
        }
        else if (_worst3.Entity_base.Tier == 1 && _worst3.Age >= 12 && _worst3.Balance.GetRollingYear() < 5000000 && manager == null)
        {
            scene.Session.Commands.Add(new CommandSell(company.ID, _worst3.ID, _worst3.Type, manager));
        }
    }



}

