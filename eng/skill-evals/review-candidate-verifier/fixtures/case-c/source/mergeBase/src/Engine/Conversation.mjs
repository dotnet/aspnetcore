export class Conversation
{
  status = "Idle";
  observers = [];

  constructor(log)
  {
    this.log = log;
  }

  onStatusChanged(observer)
  {
    this.observers.push(observer);
  }

  process(work)
  {
    this.status = "Running";
    try
    {
      work();
      this.status = "Idle";
    }
    catch (processingError)
    {
      try
      {
        this.log(processingError);
      }
      finally
      {
        this.status = "Error";
        for (const observer of this.observers)
        {
          observer(this.status);
        }
      }
    }
  }
}
